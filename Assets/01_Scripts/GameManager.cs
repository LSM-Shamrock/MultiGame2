using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


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

[AutoInjectionTarget]
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance => _instance ?? (_instance = FindAnyObjectByType<GameManager>());

    [SerializeField, AssetField("Player")] 
    private GameObject _playerPrefab;

    private const int MAXPLAYERS = 2;
    private const string SCENE_NAME_TO_CHANGE = "GameScene";

    public ObservableValue<bool> IsMatchingInProgress { get; private set; } = new();
    
    private ObservableArray<int> _currentDeck;
    public ObservableArray<int> CurrentDeckCardIds
    {
        get
        {
            if (_currentDeck == null)
            {
                _currentDeck = new ObservableArray<int>(8);

                for (int i = 0; i < 8; i++) 
                    _currentDeck[i] = StaticDB.Instance.CardDataList[i].CardId;
            }
            return _currentDeck;
        }
    }
    public string PlayerName { get; set; }
    public string LobbyId => _lobby.Id;
    private Lobby _lobby;
    private LobbyEventCallbacks _lobbyEventCallbacks = new();
    private ILobbyEvents _lobbyEvents;
    private float _heartbeatTimer;
    private bool _isHeartbeating;
    private const string LOBBY_NAME = "Lobby";
    private const string LOBBY_KEY_JOINCODE = "JoinCode";
    private const float HEARTBEAT_INTERVAL = 15f;

    public ulong LocalClientId { get; private set; }
    public ulong OpponentClientId { get; private set; }
    public Dictionary<ulong, Player> Players { get; private set; } = new();
    public Player LocalPlayer => Players[LocalClientId];
    public Player OpponentPlayer => Players[OpponentClientId];

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this);

        _lobbyEventCallbacks.PlayerDataAdded += OnLobbyPlayerDataAdded;
    }
    private async void Update()
    {
        await HeartbeatAsync();
    }

    #region Lobby
    public async Task CreateLobbyAsync(bool isPrivate = true)
    {
        IsMatchingInProgress.Value = true;

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MAXPLAYERS - 1);
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
    }
    public async Task<bool> JoinLobbyAsync(string lobbyId)
    {
        IsMatchingInProgress.Value = true;

        try
        {
            _lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            _lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(_lobby.Id, _lobbyEventCallbacks);

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(_lobby.Data[LOBBY_KEY_JOINCODE].Value);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData);
            NetworkManager.Singleton.StartClient();
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            return true;
        }
        catch (LobbyServiceException ex)
        {
            Debug.Log(ex);

            IsMatchingInProgress.Value = false;

            return false;
        }
    }
    public async Task AutoMatchingAsync()
    {
        IsMatchingInProgress.Value = true;

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
            joined = await JoinLobbyAsync(query.Results[0].Id);
            Debug.Log("찾은 로비로 접속 시도 결과 " + joined);
        }
        if (!joined)
        {
            await CreateLobbyAsync(isPrivate: false);
            Debug.Log("로비 새로 생성함");
        }
    }
    public async Task CancelMatcingAsync()
    {
        if (NetworkManager.Singleton == null) 
            return;

        if (_lobby != null)
        {
            await _lobbyEvents?.UnsubscribeAsync();

            if (_lobby.HostId == AuthenticationService.Instance.PlayerId)
                await LobbyService.Instance.DeleteLobbyAsync(_lobby.Id);
            else
                await LobbyService.Instance.RemovePlayerAsync(_lobby.Id, AuthenticationService.Instance.PlayerId);

            _lobby = null;
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.Shutdown();

        IsMatchingInProgress.Value = false;
    }

    private async Task HeartbeatAsync()
    {
        if (_isHeartbeating) return;
        if (_lobby == null) return;
        if (_lobby.HostId != AuthenticationService.Instance.PlayerId) return;

        _heartbeatTimer += Time.deltaTime;
        if (_heartbeatTimer >= HEARTBEAT_INTERVAL)
        {
            _heartbeatTimer = 0f;
            _isHeartbeating = true;
            await LobbyService.Instance.SendHeartbeatPingAsync(_lobby.Id);
            _isHeartbeating = false;
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
    private void OnLobbyPlayerDataAdded(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> playerDatas)
    {
        foreach (var (playerIndex, playerData) in playerDatas)
        {
            foreach (var (dataKey, dataValue) in playerData)
            {
                string dataString = dataValue.Value.Value;
                
                switch (dataKey)
                {
                    case "PlayerSessionData":
                        PlayerSessionData obj = JsonConvert.DeserializeObject<PlayerSessionData>(dataString);

                        if (obj.ClientId != NetworkManager.Singleton.LocalClientId)
                        {
                            LocalClientId = obj.ClientId;
                            Debug.Log($"상대 플레이어 세션 데이터 할당됨. \n{dataString}");
                        }
                        else
                        {
                            OpponentClientId = obj.ClientId;
                            Debug.Log($"로컬 플레이어 세션 데이터 할당됨. \n{dataString}");
                        }

                        if (NetworkManager.Singleton.IsHost)
                            SpawnPlayer(obj.ClientId, obj.PlayerName, obj.DeckCardIds);
                        break;
                }
            }
        }
    }
    #endregion

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            IsMatchingInProgress.Value = false;
        }
    }
    private async void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count == MAXPLAYERS)
            await UploadLobbyPlayerDataAsync();
    }

    private void SpawnPlayer(ulong clientId, string playerName, int[] deckCardIds)
    {
        GameObject go = Instantiate(_playerPrefab);
        NetworkObject obj = go.GetComponent<NetworkObject>();
        Player player = go.GetComponent<Player>();
        player.Init(playerName, deckCardIds);
        player.OnNetworkSpawned += OnPlayerNetworkSpawned;
        obj.SpawnAsPlayerObject(clientId);
    }
    private async void OnPlayerNetworkSpawned(Player player)
    {
        Players[player.OwnerClientId] = player;

        if (Players.Count == MAXPLAYERS)
            await TryStartGameAsync();
    }
    private async Task<bool> TryStartGameAsync()
    {
        if (!NetworkManager.Singleton.IsHost) return false;
        if (NetworkManager.Singleton.ConnectedClients.Count != MAXPLAYERS) return false;
        if (Players.Count < 2) return false;

        // 더 이상 참가자 받지 않도록 로비 잠금
        if (_lobby != null)
        {
            await LobbyService.Instance.UpdateLobbyAsync(_lobby.Id, new UpdateLobbyOptions { IsLocked = true });
            await _lobbyEvents.UnsubscribeAsync();
            _lobby = null;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(SCENE_NAME_TO_CHANGE, LoadSceneMode.Single);
        
        return true;
    }
}
