using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class GameScene : MonoBehaviour
{
    public static GameScene Instance => _instance ?? (_instance = FindAnyObjectByType<GameScene>());
    private static GameScene _instance;

    [SerializeField, AssetField("Player")] private GameObject _playerPrefab;
    [SerializeField, AssetField("Core")] private GameObject _corePrefab;
    
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
                SpawnPlayer(v.ClientId, v.PlayerName, v.DeckCardIds);
        }
    }

    private void SpawnPlayer(ulong clientId, string playerName, int[] deckCardIds)
    {
        GameObject go = Instantiate(_playerPrefab);
        NetworkObject obj = go.GetComponent<NetworkObject>();
        Player player = go.GetComponent<Player>();
        player.Init(playerName, deckCardIds);
        obj.SpawnAsPlayerObject(clientId);
    }
}
