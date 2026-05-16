using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyController : ProjectBehaviour
{
    [SerializeField, ChildField] private LobbyUI LobbyUI;
    [SerializeField, ChildField] private MatchmakingUI MatchmakingUI;

    private const int _maxPlayers = 2;
    private string _sceneNameToChange = "GameScene";


    private void Start()
    {
        LobbyUI.CreateButton.onClick.AddListener(OnClick_CreateButton);
        LobbyUI.JoinButton.onClick.AddListener(OnClick_JoinButton);
        MatchmakingUI.CancleButton.onClick.AddListener(OnClick_CancleButton);
    }


    private async void OnClick_CreateButton()
    {
        string joinCode = await CreateRoomAsync();

        MatchmakingUI.JoinCodeText.text = joinCode;
        MatchmakingUI.gameObject.SetActive(true);
    }
    private async void OnClick_JoinButton()
    {
        string joinCode = LobbyUI.JoinCodeInput.text;

        if (await JoinRoomAsync(joinCode))
        {
            MatchmakingUI.JoinCodeText.text = joinCode;
            MatchmakingUI.gameObject.SetActive(true);
        }
    }
    private void OnClick_CancleButton()
    {
        CancelRoom();
        MatchmakingUI.gameObject.SetActive(false);
    }


    public async Task<string> CreateRoomAsync()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

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
    public async Task<bool> JoinRoomAsync(string joinCode)
    {
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        if (allocation == null)
            return false;

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
    public void CancelRoom()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.Shutdown();
    }


    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClients.Count >= _maxPlayers)
            NetworkManager.Singleton.SceneManager.LoadScene(_sceneNameToChange, LoadSceneMode.Single);
    }
}
