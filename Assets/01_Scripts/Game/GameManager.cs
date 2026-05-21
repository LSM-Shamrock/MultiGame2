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
    [SerializeField, AssetField("PlayerCore")] private GameObject PlayerCorePrefab;

    public IReadOnlyList<ulong> ClientIds { get; private set; } 

    private void Start()
    {
        _instance = this;

        ClientIds = NetworkManager.Singleton.ConnectedClientsIds;

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
        obj.SpawnAsPlayerObject(clientId);
    }
}
