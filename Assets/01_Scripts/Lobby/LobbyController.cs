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

public class LobbyController : ProjectBehaviour
{
    [SerializeField, ChildField] private LobbyUI LobbyUI;
    [SerializeField, ChildField] private MatchmakingUI MatchmakingUI;

    private const int MAXPLAYERS = 2;
    private string SCENE_NAME_TO_CHANGE = "GameScene";
    private string _joinCode;

    private Lobby _autoMatchingLobby;
    private float _heartbeatTimer;
    private bool _isHeartbeating;
    private const string JOIN_CODE_KEY = "joinCode";
    private const string LOBBY_NAME = "AutoMatch";
    private const float HEARTBEAT_INTERVAL = 15f;


    private void Start()
    {
        LobbyUI.CreateButton.onClick.AddListener(OnClick_CreateButton);
        LobbyUI.JoinButton.onClick.AddListener(OnClick_JoinButton);
        LobbyUI.PlayButton.onClick.AddListener(OnClick_AutoMatching);
        MatchmakingUI.CancleButton.onClick.AddListener(OnClick_CancleButton);
    }
    private void Update()
    {
        HeartbeatLobby();
    }


    private async void OnClick_CreateButton()
    {
        await CreateRoomAndCodeAsync();

        MatchmakingUI.JoinCodeText.text = _joinCode;
        MatchmakingUI.gameObject.SetActive(true);
    }
    private async void OnClick_JoinButton()
    {
        string inputJoinCode = LobbyUI.JoinCodeInput.text;

        if (await JoinRoomWithCodeAsync(inputJoinCode))
        {
            MatchmakingUI.JoinCodeText.text = inputJoinCode;
            MatchmakingUI.gameObject.SetActive(true);
        }
    }
    private async void OnClick_AutoMatching()
    {
        await AutoMatchingAsync();

        MatchmakingUI.JoinCodeText.text = "매치메이킹";
        MatchmakingUI.gameObject.SetActive(true);
    }
    private void OnClick_CancleButton()
    {
        CancelRoom();
        MatchmakingUI.gameObject.SetActive(false);
    }


    public async Task CreateRoomAndCodeAsync()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MAXPLAYERS - 1);

        _joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

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
    }
    public async Task<bool> JoinRoomWithCodeAsync(string joinCode)
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
    
    public void CancelRoom()
    {
        if (NetworkManager.Singleton == null) 
            return;

        if (_autoMatchingLobby != null)
        {
            if (NetworkManager.Singleton.IsHost)
                LobbyService.Instance.DeleteLobbyAsync(_autoMatchingLobby.Id);
            else
                LobbyService.Instance.RemovePlayerAsync(_autoMatchingLobby.Id, AuthenticationService.Instance.PlayerId);

            _autoMatchingLobby = null;
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.Shutdown();
    }


    public async Task AutoMatchingAsync()
    {
        // 빈 로비 탐색
        QueryResponse query = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
        {
            Filters = new List<QueryFilter>
            {
                // 빈 슬롯이 1개 이상인 로비만
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            }
        });

        if (query.Results.Count > 0)
        {
            bool joined = await AutoMatching_JoinLobbyAsync(query.Results[0]);
            if (!joined)
                await AutoMatching_CreateLobbyAsync();

        }
        else
        {
            await AutoMatching_CreateLobbyAsync();
        }
    }
    private async Task AutoMatching_CreateLobbyAsync()
    {
        await CreateRoomAndCodeAsync();

        CreateLobbyOptions options = new CreateLobbyOptions()
        {
            IsPrivate = false,
            Data = new Dictionary<string, DataObject>
            {
                { JOIN_CODE_KEY, new DataObject(DataObject.VisibilityOptions.Public, _joinCode) }
            }
        };

        _autoMatchingLobby = await LobbyService.Instance.CreateLobbyAsync(LOBBY_NAME, MAXPLAYERS, options);
    }
    private async Task<bool> AutoMatching_JoinLobbyAsync(Lobby lobby)
    {
        try
        {
            _autoMatchingLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);

            _joinCode = _autoMatchingLobby.Data[JOIN_CODE_KEY].Value;

            return await JoinRoomWithCodeAsync(_joinCode);
        }
        catch (LobbyServiceException)
        {
            return false;
        }
        
    }
    private async void HeartbeatLobby()
    {
        if (_isHeartbeating) return;
        if (_autoMatchingLobby == null) return;
        if (!NetworkManager.Singleton.IsHost) return;

        _heartbeatTimer += Time.deltaTime;
        if (_heartbeatTimer >= HEARTBEAT_INTERVAL)
        {
            _heartbeatTimer = 0f;
            _isHeartbeating = true;
            await LobbyService.Instance.SendHeartbeatPingAsync(_autoMatchingLobby.Id);
            _isHeartbeating = false;
        }
    }


    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
    private async void OnClientConnected(ulong clientId)
    {
        // 게임 시작
        if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClients.Count >= MAXPLAYERS)
        {
            // 더 이상 참가자 받지 않도록 로비 삭제
            if (_autoMatchingLobby != null)
            {
                await LobbyService.Instance.DeleteLobbyAsync(_autoMatchingLobby.Id);
                _autoMatchingLobby = null;
            }

            NetworkManager.Singleton.SceneManager.LoadScene(SCENE_NAME_TO_CHANGE, LoadSceneMode.Single);
        }
    }
}
