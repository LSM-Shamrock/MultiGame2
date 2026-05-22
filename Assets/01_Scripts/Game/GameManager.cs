using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[AutoInjectionTarget]
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance => _instance ?? (_instance = FindAnyObjectByType<GameManager>());

    [SerializeField, ChildField] private Transform RotateRoot;
    [SerializeField, ChildField] private Transform CoreSpawnPos1;
    [SerializeField, ChildField] private Transform CoreSpawnPos2;
    [SerializeField, ChildField] private GameUI GameUI;
    [SerializeField, AssetField("PlayerCore")] private GameObject PlayerCorePrefab;

    public IReadOnlyList<ulong> ClientIds => NetworkManager.Singleton.ConnectedClientsIds;

    public Dictionary<ulong, PlayerCore> PlayerCores = new();
    public ulong LocalClientId => LobbyManager.Instance.LocalPlayerSessionData.ClientId;
    public ulong OpponentClientId => LobbyManager.Instance.OpponentPlayerSessionData.ClientId;
    public PlayerCore LocalPlayerCore { get; private set; }
    public PlayerCore OpponentPlayerCore { get; private set; }

    private void Start()
    {
        _instance = this;

        Debug.Log($"로컬 클라 Id : {LocalClientId}, 상대 클라 Id : {OpponentClientId}");


        if (NetworkManager.Singleton.IsHost)
        {
            SpawnCore(CoreSpawnPos1, ClientIds[0]);
            SpawnCore(CoreSpawnPos2, ClientIds[1]);
        }
        else
        {
            RotateRoot.Rotate(0, 180, 0);
        }
    }


    private void SpawnCore(Transform spawnPos, ulong clientId)
    {
        GameObject go = Instantiate(PlayerCorePrefab, spawnPos.position, spawnPos.rotation);
        NetworkObject obj = go.GetComponent<NetworkObject>();
        PlayerCore core = go.GetComponent<PlayerCore>();
        obj.SpawnAsPlayerObject(clientId);
    }

    public void OnPlayerCoreSpawned(PlayerCore playerCore)
    {
        PlayerCores[playerCore.OwnerClientId] = playerCore;

        if (playerCore.IsOwner)
        {
            LocalPlayerCore = playerCore;
            GameUI.Initialize();
            LocalPlayerCore.SetupDatas();
        }
        else
        {
            OpponentPlayerCore = playerCore;
        }
    }
}
