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

[Serializable] 
public struct MatchingFilterData
{
    public string MatchingVersion;
    public bool IsAutoMatching;
}

public enum MatchingManagerState
{
    Lobby,
    FindingMatching,
    CreateingMatching,
    JoiningMatching,
    WaitingForPalyers,
    CancellingMatching,
    StartingGame,
}

public enum MatchingType
{
    None,
    AutoMatching,
    LobbyIdMatching,
    PvE,
}

[AutoInjectionTarget]
public class MatchingManager : SingletonBehaviour<MatchingManager>
{
    private const int MAXPLAYERS = 2;
    private const string SCENE_GAME = "GameScene";
    private const string SCENE_LOBBY = "LobbyScene";

    public IObservOnlyValue<MatchingManagerState> State => _state;
    private ObservableValue<MatchingManagerState> _state = new (MatchingManagerState.Lobby);
    public MatchingType MatchingType
    {
        get
        {
            return _state.Value switch
            {
                MatchingManagerState.CreateingMatching => _matchingType,
                MatchingManagerState.WaitingForPalyers => _matchingType,
                _ => MatchingType.None,
            };
        }
    }
    private MatchingType _matchingType;

    public PlayerSessionData LocalPlayerSessionData { get; private set; }
    public PlayerSessionData OpponentPlayerSessionData { get; private set; }

    public string MatchingFilter { get; private set; }
    public string LobbyId => _lobby.Id;
    private Lobby _lobby;
    private ILobbyEvents _lobbyEvents;
    private LobbyEventCallbacks _lobbyEventCallbacks = new();
    private const float                     LOBBY_HEARTBEAT_INTERVAL = 15f;
    private const string                    LOBBY_NAME = "Lobby";
    private const string                    LOBBY_DATA_JOINCODE = "JoinCode";
    private const string                    LOBBY_DATA_MATCHINGFILTER = "FilterData";
    private const DataObject.IndexOptions   LOBBY_DATA_MATCHINGFILTER_INDEX = DataObject.IndexOptions.S1;
    private const QueryFilter.FieldOptions  LOBBY_DATA_MATCHINGFILTER_FILTER = QueryFilter.FieldOptions.S1;

    private void SetMatchingFilter(MatchingType matchingType)
    {
        _matchingType = matchingType;

        MatchingFilterData filterData = new MatchingFilterData
        {
            MatchingVersion = RemoteConfigManager.Instance.GameDataVersion.Value,
            IsAutoMatching = matchingType == MatchingType.AutoMatching,
        };
        MatchingFilter = JsonConvert.SerializeObject(filterData);
    }
    private async Task CreateMatchingAsync()
    {
        _state.Value = MatchingManagerState.CreateingMatching;
        
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
            IsPrivate = false,
            Data = new Dictionary<string, DataObject>
            {
                { LOBBY_DATA_JOINCODE, new(DataObject.VisibilityOptions.Member, joinCode) },
                { LOBBY_DATA_MATCHINGFILTER, new(DataObject.VisibilityOptions.Public, MatchingFilter, LOBBY_DATA_MATCHINGFILTER_INDEX) },
            },
        });
        _lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(_lobby.Id, _lobbyEventCallbacks);

        _state.Value = MatchingManagerState.WaitingForPalyers;
    }
    private async Task<bool> JoinMatchingAsync(string lobbyId)
    {
        _state.Value = MatchingManagerState.JoiningMatching;

        var lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
        if (lobby != null && lobby.Data[LOBBY_DATA_MATCHINGFILTER].Value.Equals(MatchingFilter) == false)
        {
            Debug.LogWarning("입장하려는 방과 필터가 달라서 입장 실패함");
            _state.Value = MatchingManagerState.Lobby;
            return false;
        }

        try
        {
            _lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            _lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(_lobby.Id, _lobbyEventCallbacks);
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);
            _state.Value = MatchingManagerState.Lobby;
            return false;
        }

        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(_lobby.Data[LOBBY_DATA_JOINCODE].Value);
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
    
    private async Task DeleteLobbyAsync()
    {
        if (_lobby.HostId != AuthenticationService.Instance.PlayerId)
            return;

        await _lobbyEvents?.UnsubscribeAsync();
        await LobbyService.Instance.DeleteLobbyAsync(_lobby.Id);

        _lobbyEvents = null;
        _lobby = null;
    }
    private async Task ShutdownClientAsync()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.Shutdown();

        while (NetworkManager.Singleton.IsListening)
            await Task.Yield();
    }
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
        await ShutdownClientAsync();

        _state.Value = MatchingManagerState.Lobby;

        await SceneManager.LoadSceneAsync(SCENE_LOBBY);
    }

    public async Task CancelMatcingAsync()
    {
        if (_state.Value != MatchingManagerState.WaitingForPalyers)
            return;

        _state.Value = MatchingManagerState.CancellingMatching;

        await DeleteLobbyAsync();
        await ShutdownClientAsync();

        _state.Value = MatchingManagerState.Lobby;
    }
    public async Task CreateLobbyIdAsync()
    {
        SetMatchingFilter(MatchingType.LobbyIdMatching);

        await CreateMatchingAsync();
    }
    public async Task JoinWithLobbyIdAsync(string lobbyId)
    {
        SetMatchingFilter(MatchingType.LobbyIdMatching);

        await JoinMatchingAsync(lobbyId);
    }
    public async Task AutoMatchingAsync()
    {
        SetMatchingFilter(MatchingType.AutoMatching);

        _state.Value = MatchingManagerState.FindingMatching;

        QueryResponse query = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
        {
            Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                new QueryFilter(LOBBY_DATA_MATCHINGFILTER_FILTER, MatchingFilter, QueryFilter.OpOptions.EQ)
            }
        });

        bool joined = false;
        foreach (Lobby lobby in query.Results)
        {
            joined = await JoinMatchingAsync(lobby.Id);
            Debug.Log("찾은 로비로 접속 시도 결과 " + joined);

            if (joined)
                break;
        }
        
        if (!joined)
        {
            await CreateMatchingAsync();
            Debug.Log("로비 새로 생성함");
        }
    }
    public async Task PvEAsync()
    {

    }

    private async Task UploadPlayerSessionDataAsync()
    {
        PlayerSessionData data = new PlayerSessionData(
            lobbyPlayerId: _lobby.Id,
            clientId: NetworkManager.Singleton.LocalClientId,
            playerName: LobbyManager.Instance.PlayerName,
            deckCardIds: LobbyManager.Instance.CurrentDeckCardIds.Values.ToArray());

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
    private void Awake()
    {
        InitSingleton();

        _lobbyEventCallbacks.PlayerDataAdded += OnLobbyPlayerDataAdded;
        _lobbyEventCallbacks.LobbyDeleted += OnLobbyDeleted;

        StartCoroutine(HeartbeatRoutine());
    }
    private async void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count == MAXPLAYERS)
        {
            _state.Value = MatchingManagerState.StartingGame;
            await UploadPlayerSessionDataAsync();
        }
    }
    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

            _state.Value = MatchingManagerState.Lobby;
        }
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
    private void OnLobbyDeleted()
    {
        _lobbyEvents = null;
        _lobby = null;
    }
}
