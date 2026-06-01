using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class GameScene : MonoBehaviour
{
    public static GameScene Instance => _instance ?? (_instance = FindAnyObjectByType<GameScene>());
    private static GameScene _instance;

    [SerializeField, AssetField("Player")]
    private GameObject _playerPrefab;
    [SerializeField, AssetField("Bgm_Game")]
    private AudioClip _gameBgm;

    public ObservableValue<Player> LocalPlayer { get; private set; } = new();
    public ObservableValue<Player> OpponentPlayer { get; private set; } = new();

    private void Start()
    {
        _instance = this;

        if (NetworkManager.Singleton.IsHost && GameManager.Instance)
        {
            foreach (var (k, v) in GameManager.Instance.PlayerSessionDatas)
                SpawnPlayer(v.ClientId, v.PlayerName, v.DeckCardIds, v.ClientId != NetworkManager.Singleton.LocalClientId);
        }

        SoundManager.Instance.PlayBgm(_gameBgm);
    }

    private void SpawnPlayer(ulong clientId, string playerName, int[] deckCardIds, bool isRotate)
    {
        GameObject go = Instantiate(_playerPrefab, Vector2.zero, isRotate ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);
        NetworkObject obj = go.GetComponent<NetworkObject>();
        Player player = go.GetComponent<Player>();
        player.Init(playerName, deckCardIds);
        obj.SpawnAsPlayerObject(clientId);
    }
}
