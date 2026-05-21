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
    public readonly string LobbyId;
    public readonly ulong ClientId;
    public readonly string PlayerName;

    [JsonConstructor]
    public PlayerSessionData(string lobbyId, ulong clientId, string playerName)
    {
        LobbyId = lobbyId;
        ClientId = clientId;
        PlayerName = playerName;
    }
}

[AutoInjectionTarget]
public class LobbyManager : MonoBehaviour
{
    private static LobbyManager _instance;
    public static LobbyManager Instance => _instance ?? (_instance = FindAnyObjectByType<LobbyManager>());

    private const int MAXPLAYERS = 2;
    private const string SCENE_NAME_TO_CHANGE = "GameScene";

    public ObservableValue<bool> IsMatchingInProgress { get; private set; } = new();
    
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

    public ObservableArray<CardData> CurrentDeck
    {
        get
        {
            if (_currentDeck == null)
            {
                _currentDeck = new ObservableArray<CardData>(8);

                for (int i = 0; i < 8; i++) 
                    _currentDeck[i] = StaticDB.Instance.CardDataList[i];
            }
            return _currentDeck;
        }
    }
    private ObservableArray<CardData> _currentDeck;

    private Dictionary<ulong, PlayerSessionData> _playerSessionDatas = new();
    public IReadOnlyDictionary<ulong, PlayerSessionData> PlayerSessionDatas => _playerSessionDatas;

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

    public async Task CreateLobbyAsync(bool isPrivate = true)
    {
        IsMatchingInProgress.Value = true;

        string joinCode = await CreateRoomAsync();

        CreateLobbyOptions options = new CreateLobbyOptions()
        {
            IsPrivate = isPrivate,
            Data = new Dictionary<string, DataObject>
            {
                { LOBBY_KEY_JOINCODE, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
            }
        };

        _lobby = await LobbyService.Instance.CreateLobbyAsync(LOBBY_NAME, MAXPLAYERS, options);
        _lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(_lobby.Id, _lobbyEventCallbacks);
    }
    public async Task<bool> JoinLobbyAsync(string lobbyId)
    {
        IsMatchingInProgress.Value = true;

        try
        {
            _lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            _lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(_lobby.Id, _lobbyEventCallbacks);

            return await JoinRoomAsync(_lobby.Data[LOBBY_KEY_JOINCODE].Value);
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
        _playerSessionDatas.Clear();
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
        PlayerSessionData data = new PlayerSessionData(_lobby.Id, NetworkManager.Singleton.LocalClientId, PlayerName);
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
                
                Debug.Log($"로비 플레이어 데이터 할당됨. \n데이터 키: {dataKey} 데이터: {dataString}");

                switch (dataKey)
                {
                    case "PlayerSessionData":
                        PlayerSessionData obj = JsonConvert.DeserializeObject<PlayerSessionData>(dataString);
                        _playerSessionDatas[obj.ClientId] = obj;
                        await StartGameAsync();
                        break;
                }
            }
        }
    }

    private async Task<string> CreateRoomAsync()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MAXPLAYERS - 1);

        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData);

        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        return joinCode;
    }
    private async Task<bool> JoinRoomAsync(string joinCode)
    {
        if (string.IsNullOrEmpty(joinCode)) 
            return false; 

        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
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
        catch (RelayServiceException ex)
        {
            Debug.Log(ex);

            return false;
        }
    }
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

    private async Task StartGameAsync()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        if (NetworkManager.Singleton.ConnectedClients.Count != MAXPLAYERS) return;
        if (_playerSessionDatas.Count != MAXPLAYERS) return;

        // 더 이상 참가자 받지 않도록 로비 삭제
        if (_lobby != null)
        {
            await LobbyService.Instance.DeleteLobbyAsync(_lobby.Id);
            _lobby = null;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(SCENE_NAME_TO_CHANGE, LoadSceneMode.Single);


        foreach (var data in _playerSessionDatas.Values)
            Debug.Log($"{JsonConvert.SerializeObject(data)}");
    }
}
