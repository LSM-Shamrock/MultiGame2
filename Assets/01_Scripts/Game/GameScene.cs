using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class GameScene : MonoBehaviour
{
    public static GameScene Instance => _instance ?? (_instance = FindAnyObjectByType<GameScene>());
    private static GameScene _instance;

    [SerializeField, AssetField("Player")] private GameObject _playerPrefab;
    [SerializeField, AssetField("Core")] private GameObject _corePrefab;
    [SerializeField, ChildField] private Transform CoreSpawnPos1;
    [SerializeField, ChildField] private Transform CoreSpawnPos2;
    
    public ObservableValue<Player> LocalPlayer { get; private set; } = new();
    public ObservableValue<Player> OpponentPlayer { get; private set; } = new();

    private void Awake()
    {
        _instance = this;
    }
    private void Start()
    {
        if (NetworkManager.Singleton.IsHost && GameManager.Instance)
        {
            foreach (var (k, v) in GameManager.Instance.PlayerSessionDatas)
                SpawnPlayer(v.ClientId, v.PlayerName, v.DeckCardIds, v.ClientId != NetworkManager.Singleton.LocalClientId);

            SpawnCore(GameManager.Instance.LocalClientId, CoreSpawnPos1);
            SpawnCore(GameManager.Instance.OpponentClientId, CoreSpawnPos2);
        }
    }

    private void SpawnPlayer(ulong clientId, string playerName, int[] deckCardIds, bool isRotate)
    {
        GameObject go = Instantiate(_playerPrefab, Vector2.zero, isRotate ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);
        NetworkObject obj = go.GetComponent<NetworkObject>();
        Player player = go.GetComponent<Player>();
        player.Init(playerName, deckCardIds);
        obj.SpawnAsPlayerObject(clientId);
    }
    private void SpawnCore(ulong clientId, Transform pos)
    {
        GameObject go = Instantiate(_corePrefab, pos.position, pos.rotation);
        NetworkObject obj = go.GetComponent<NetworkObject>();
        Core core = go.GetComponent<Core>();
        obj.SpawnWithOwnership(clientId);
    }
}
