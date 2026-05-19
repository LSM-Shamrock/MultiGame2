using System.Collections.Generic;
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


[AutoInjectionTarget]
public class LobbyManager : MonoBehaviour
{
    private static LobbyManager _instance;
    public static LobbyManager Instance => _instance ?? (_instance = FindAnyObjectByType<LobbyManager>());

    [SerializeField, ChildField] private LobbyUI LobbyUI;
    [SerializeField, ChildField] private MatchmakingUI MatchmakingUI;

    private const int MAXPLAYERS = 2;
    private const string SCENE_NAME_TO_CHANGE = "GameScene";

    public ObservableValue<bool> IsMatchingInProgress { get; private set; } = new();

    public string LobbyId => _currentLobby.Id;
    private Lobby _currentLobby;
    private float _heartbeatTimer;
    private bool _isHeartbeating;
    private const string LOBBY_NAME = "AutoMatch";
    private const string JOIN_CODE_KEY = "JoinCode";
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


    private void Awake()
    {
        _instance = this;
    }

    private void Update()
    {
        Heartbeat();
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

            return true;
        }
        catch (RelayServiceException ex)
        {
            Debug.Log(ex);

            return false;
        }
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
                { JOIN_CODE_KEY, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
            }
        };

        _currentLobby = await LobbyService.Instance.CreateLobbyAsync(LOBBY_NAME, MAXPLAYERS, options);
    }
    public async Task<bool> JoinLobbyAsync(string lobbyId)
    {
        IsMatchingInProgress.Value = true;

        try
        {
            _currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            return await JoinRoomAsync(_currentLobby.Data[JOIN_CODE_KEY].Value);
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
    
    public void CancleRoom()
    {
        if (NetworkManager.Singleton == null) 
            return;

        if (_currentLobby != null)
        {
            if (NetworkManager.Singleton.IsHost)
                LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
            else
                LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, AuthenticationService.Instance.PlayerId);

            _currentLobby = null;
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.Shutdown();

        IsMatchingInProgress.Value = false;
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
        // 게임 시작
        if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClients.Count >= MAXPLAYERS)
        {
            // 더 이상 참가자 받지 않도록 로비 삭제
            if (_currentLobby != null)
            {
                await LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
                _currentLobby = null;
            }

            NetworkManager.Singleton.SceneManager.LoadScene(SCENE_NAME_TO_CHANGE, LoadSceneMode.Single);
        }
    }

    private async void Heartbeat()
    {
        if (_isHeartbeating) return;
        if (_currentLobby == null) return;
        if (_currentLobby.HostId != AuthenticationService.Instance.PlayerId) return;

        _heartbeatTimer += Time.deltaTime;
        if (_heartbeatTimer >= HEARTBEAT_INTERVAL)
        {
            _heartbeatTimer = 0f;
            _isHeartbeating = true;
            await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
            _isHeartbeating = false;
        }
    }
}
