using System;
using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class GameScene : NetworkBehaviour, ISceneInstance<GameScene>
{
    [SerializeField, AssetField("Player")]
    private GameObject _playerPrefab;

    [SerializeField, ChildField("RotationRoot")]
    private Transform RotationRoot;

    public ObservableValue<Player> LocalPlayer { get; private set; } = new();
    public ObservableValue<Player> OpponentPlayer { get; private set; } = new();

    public bool IsGameFinished { get; private set; } = false;
    public event Action<ulong?> OnGameFinished;

    private void Start()
    {
        ((ISceneInstance<GameScene>)this).InitSceneInstance();

        if (NetworkManager.Singleton.IsHost && GameManager.Instance)
        {
            var local = GameManager.Instance.LocalPlayerSessionData;
            var opponent = GameManager.Instance.OpponentPlayerSessionData;
            SpawnPlayer(local.ClientId, local.PlayerName, local.DeckCardIds, false);
            SpawnPlayer(opponent.ClientId, opponent.PlayerName, opponent.DeckCardIds, true);
        }

        if (IsHost == false)
            RotationRoot.rotation = Quaternion.Euler(0, 180, 0);
    }
    private void Update()
    {
        if (IsServer)
        {
            if (LocalPlayer.Value != null && OpponentPlayer.Value != null && !IsGameFinished)
            {
                if (LocalPlayer.Value.IsDead && OpponentPlayer.Value.IsDead) FinishGameRpc(isDraw: true);
                else if (LocalPlayer.Value.IsDead) FinishGameRpc(isDraw: false, OpponentPlayer.Value.OwnerClientId);
                else if (OpponentPlayer.Value.IsDead) FinishGameRpc(isDraw: false, LocalPlayer.Value.OwnerClientId);
            }
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

    [Rpc(SendTo.ClientsAndHost)]
    private void FinishGameRpc(bool isDraw, ulong winnerClientId = 0)
    {
        if (IsGameFinished)
            return;

        IsGameFinished = true;
        OnGameFinished?.Invoke(isDraw ? null : winnerClientId);
    }
}
