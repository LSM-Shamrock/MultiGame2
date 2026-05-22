using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[AutoInjectionTarget]
public class GameManager : NetworkBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance => _instance ?? (_instance = FindAnyObjectByType<GameManager>());

    [SerializeField, ChildField] private Transform RotateRoot;
    [SerializeField, ChildField] private Transform CoreSpawnPos1;
    [SerializeField, ChildField] private Transform CoreSpawnPos2;
    [SerializeField, ChildField] private GameUI GameUI;
    [SerializeField, AssetField("PlayerCore")] private GameObject PlayerCorePrefab;

    public IReadOnlyList<ulong> ClientIds { get; private set; }
    public Dictionary<ulong, PlayerCore> PlayerCores = new();

    public ulong LocalClientId { get; private set; }
    public ulong OpponentClientId { get; private set; }
    public PlayerCore LocalPlayerCore => PlayerCores[LocalClientId];
    public PlayerCore OpponentPlayerCore => PlayerCores[OpponentClientId];


    private void Awake()
    {
        _instance = this;

        ClientIds = NetworkManager.Singleton.ConnectedClientsIds;
        LocalClientId = LobbyManager.Instance.LocalPlayerSessionData.ClientId;
        OpponentClientId = LobbyManager.Instance.OpponentPlayerSessionData.ClientId;

        if (IsHost)
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
}
