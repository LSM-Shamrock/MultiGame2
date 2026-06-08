using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;


[Serializable]
public class PlayerSessionData
{
    public readonly string LobbyPlayerId;
    public readonly ulong ClientId;
    public readonly string PlayerName;
    public readonly int[] DeckCardIds;

    [JsonConstructor]
    public PlayerSessionData(string lobbyPlayerId, ulong clientId, string playerName, int[] deckCardIds)
    {
        LobbyPlayerId = lobbyPlayerId;
        ClientId = clientId;
        PlayerName = playerName;
        DeckCardIds = deckCardIds;
    }
}

public enum GameManagerState
{
    Lobby,
    FindingMatching,
    CreateingMatching,
    JoiningMatching,
    WaitingForPalyers,
    CancellingMatching,
    StartingGame,
}

[AutoInjectionTarget]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance => _instance != null ? _instance : (_instance = FindAnyObjectByType<GameManager>());
    private static GameManager _instance;

    private const int MAXPLAYERS = 2;
    private const string SCENE_GAME = "GameScene";
    private const string SCENE_LOBBY = "LobbyScene";

    public IObservOnlyValue<GameManagerState> State => _state;
    private ObservableValue<GameManagerState> _state = new(GameManagerState.Lobby);

    public string PlayerName { get; set; }
    public ObservableArray<int> CurrentDeckCardIds
    {
        get
        {
            if (_currentDeck == null)
            {
                _currentDeck = new ObservableArray<int>(8);

                for (int i = 0; i < 8; i++)
                    _currentDeck[i] = StaticDB.Instance.CardData.List[i].CardId;
            }
            return _currentDeck;
        }
    }
    private ObservableArray<int> _currentDeck;

    public string LobbyId => _lobby.Id;
    private Lobby _lobby;
    private ILobbyEvents _lobbyEvents;
    private LobbyEventCallbacks _lobbyEventCallbacks = new();
    private const string LOBBY_NAME = "Lobby";
    private const string LOBBY_KEY_JOINCODE = "JoinCode";
    private const float LOBBY_HEARTBEAT_INTERVAL = 15f;

    public PlayerSessionData LocalPlayerSessionData { get; private set; }
    public PlayerSessionData OpponentPlayerSessionData { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _lobbyEventCallbacks.PlayerDataAdded += OnLobbyPlayerDataAdded;
        _lobbyEventCallbacks.LobbyDeleted += OnLobbyDeleted;

        StartCoroutine(HeartbeatRoutine());
    }

    #region Matching
    public async Task CreateMatchingAsync(bool isPrivate = true)
    {
        _state.Value = GameManagerState.CreateingMatching;

        LocalPlayerSessionData = null;
        OpponentPlayerSessionData = null;

        var allocation = await RelayService.Instance.CreateAllocationAsync(MAXPLAYERS - 1);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        _lobby = await LobbyService.Instance.CreateLobbyAsync(LOBBY_NAME, MAXPLAYERS, new CreateLobbyOptions()
        {
            IsPrivate = isPrivate,
            Data = new Dictionary<string, DataObject>
            {
                { LOBBY_KEY_JOINCODE, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
            }
        });
        _lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(_lobby.Id, _lobbyEventCallbacks);

        _state.Value = GameManagerState.WaitingForPalyers;
    }
    public async Task<bool> JoinMatchingAsync(string lobbyId)
    {
        _state.Value = GameManagerState.JoiningMatching;

        try
        {
            _lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            _lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(_lobby.Id, _lobbyEventCallbacks);

            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(_lobby.Data[LOBBY_KEY_JOINCODE].Value);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData);
            NetworkManager.Singleton.StartClient();
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            return true;
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);

            _state.Value = GameManagerState.Lobby;

            return false;
        }
    }
    public async Task AutoMatchingAsync()
    {
        _state.Value = GameManagerState.FindingMatching;

        QueryResponse query = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
        {
            Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            }
        });

        bool joined = false;
        if (query.Results.Count > 0)
        {
            joined = await JoinMatchingAsync(query.Results[0].Id);
            Debug.Log("찾은 로비로 접속 시도 결과 " + joined);
        }
        if (!joined)
        {
            await CreateMatchingAsync(isPrivate: false);
            Debug.Log("로비 새로 생성함");
        }
    }
    public async Task CancelMatcingAsync()
    {
        if (_state.Value != GameManagerState.WaitingForPalyers)
            return;

        _state.Value = GameManagerState.CancellingMatching;

        await DeleteLobbyAsync();
        await ShutdownAsync();

        _state.Value = GameManagerState.Lobby;
    }

    private IEnumerator HeartbeatRoutine()
    {
        var wait = new WaitForSeconds(LOBBY_HEARTBEAT_INTERVAL);

        while (true)
        {
            yield return wait;

            if (_lobby == null) continue;
            if (_lobby.HostId != AuthenticationService.Instance.PlayerId) continue;

            LobbyService.Instance.SendHeartbeatPingAsync(_lobby.Id);
        }
    }

    private async Task UploadLobbyPlayerDataAsync()
    {
        PlayerSessionData data = new PlayerSessionData(
            _lobby.Id,
            NetworkManager.Singleton.LocalClientId,
            PlayerName,
            CurrentDeckCardIds.Values.ToArray());

        string json = JsonConvert.SerializeObject(data);

        Debug.Log("자신의 데이터 업로드");

        await LobbyService.Instance.UpdatePlayerAsync(_lobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerSessionData", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, json) }
            }
        });
    }
    private async void OnLobbyPlayerDataAdded(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> playerDatas)
    {
        foreach (var (playerIndex, playerData) in playerDatas)
        {
            foreach (var (dataKey, dataValue) in playerData)
            {
                string dataString = dataValue.Value.Value;

                switch (dataKey)
                {
                    case "PlayerSessionData":
                        if (NetworkManager.Singleton.IsHost)
                        {
                            PlayerSessionData obj = JsonConvert.DeserializeObject<PlayerSessionData>(dataString);

                            if (obj.ClientId != NetworkManager.Singleton.LocalClientId)
                            {
                                OpponentPlayerSessionData = obj;
                                Debug.Log($"상대 플레이어 세션 데이터 할당됨. \n{dataString}");
                            }
                            else
                            {
                                LocalPlayerSessionData = obj;
                                Debug.Log($"로컬 플레이어 세션 데이터 할당됨. \n{dataString}");
                            }

                            await TryStartGameAsync();
                        }
                        break;
                }
            }
        }
    }

    private async Task DeleteLobbyAsync()
    {
        if (_lobby.HostId != AuthenticationService.Instance.PlayerId)
            return;

        await _lobbyEvents?.UnsubscribeAsync();
        await LobbyService.Instance.DeleteLobbyAsync(_lobby.Id);

        _lobbyEvents = null;
        _lobby = null;
    }
    private void OnLobbyDeleted()
    {
        _lobbyEvents = null;
        _lobby = null;
    }

    private async Task ShutdownAsync()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.Shutdown();

        while (NetworkManager.Singleton.IsListening)
            await Task.Yield();
    }
    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

            _state.Value = GameManagerState.Lobby;
        }
    }
    private async void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count == MAXPLAYERS)
        {
            _state.Value = GameManagerState.StartingGame;
            await UploadLobbyPlayerDataAsync();
        }
    }
    #endregion

    private async Task<bool> TryStartGameAsync()
    {
        if (!NetworkManager.Singleton.IsHost) return false;
        if (NetworkManager.Singleton.ConnectedClients.Count != MAXPLAYERS) return false;
        if (LocalPlayerSessionData == null || OpponentPlayerSessionData == null) return false;

        await DeleteLobbyAsync();

        NetworkManager.Singleton.SceneManager.LoadScene(SCENE_GAME, LoadSceneMode.Single);

        return true;
    }

    public async Task ExitGameToLobbyAsync()
    {
        await ShutdownAsync();

        _state.Value = GameManagerState.Lobby;

        await SceneManager.LoadSceneAsync(SCENE_LOBBY);
    }
}
