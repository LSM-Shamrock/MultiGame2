using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

[AutoInjectionTarget]
public class GameScene : NetworkBehaviour, ISceneInstance<GameScene>
{
    [SerializeField, AssetField("Player")] private GameObject _playerPrefab;
    [SerializeField, ChildField("RotationRoot")] private Transform RotationRoot;

    private PlayerSessionData _localPlayerSessionData;
    private PlayerSessionData _opponentPlayerSessionData;

    public ObservableValue<Player> LocalPlayer { get; private set; } = new();
    public ObservableValue<Player> OpponentPlayer { get; private set; } = new();

    public bool IsGameFinished { get; private set; } = false;
    public event Action<ulong?> OnGameFinished;

    private void Awake()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconect;
    }
    public override void OnDestroy()
    {
        base.OnDestroy();

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconect;
    }

    private void Start()
    {
        ((ISceneInstance<GameScene>)this).InitSceneInstance();

        if (NetworkManager.Singleton.IsHost && MatchingManager.Instance)
        {
            _localPlayerSessionData = MatchingManager.Instance.LocalPlayerSessionData;
            var localPlayer = SpawnPlayer(
                _localPlayerSessionData.ClientId, 
                _localPlayerSessionData.PlayerName, 
                _localPlayerSessionData.DeckCardIds, false);
            
            _opponentPlayerSessionData = MatchingManager.Instance.OpponentPlayerSessionData;
            var opponentPlayer = SpawnPlayer(
                _opponentPlayerSessionData.ClientId,
                _opponentPlayerSessionData.PlayerName,
                _opponentPlayerSessionData.DeckCardIds, true);
            opponentPlayer.IsBot = MatchingManager.Instance.MatchingType == MatchingType.PvE;
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

    private Player SpawnPlayer(ulong clientId, string playerName, int[] deckCardIds, bool isRotate)
    {
        GameObject go = Instantiate(_playerPrefab, Vector2.zero, isRotate ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);
        NetworkObject obj = go.GetComponent<NetworkObject>();
        Player player = go.GetComponent<Player>();
        player.Init(playerName, deckCardIds);
        obj.SpawnAsPlayerObject(clientId);
        
        return player;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void FinishGameRpc(bool isDraw, ulong winnerClientId = 0)
    {
        FinishGameLocal(isDraw, winnerClientId);
    }
    private void FinishGameLocal(bool isDraw, ulong winnerClientId = 0)
    {
        if (IsGameFinished)
            return;

        IsGameFinished = true;
        OnGameFinished?.Invoke(isDraw ? null : winnerClientId);
    }

    private void OnClientDisconect(ulong clientId)
    {
        if (!IsGameFinished)
        {
            if (IsHost)
            {
                if (clientId == _opponentPlayerSessionData.ClientId)
                {
                    Debug.Log("상대 클라이언트의 접속이 끊어져 자신의 승리로 처리함.");
                    FinishGameLocal(false, NetworkManager.LocalClientId);
                }
            }
            else
            {
                if (clientId == NetworkManager.LocalClientId)
                {
                    bool serverDown = NetworkManager.DisconnectEvent switch
                    {
                        NetworkTransport.DisconnectEvents.TransportShutdown         => false,
                        NetworkTransport.DisconnectEvents.Disconnected              => false,
                        NetworkTransport.DisconnectEvents.ProtocolTimeout           => true,
                        NetworkTransport.DisconnectEvents.MaxConnectionAttempts     => false,
                        NetworkTransport.DisconnectEvents.ClosedByRemote            => true,
                        NetworkTransport.DisconnectEvents.ClosedRemoteConnection    => false,
                        NetworkTransport.DisconnectEvents.AuthenticationFailure     => false,
                        NetworkTransport.DisconnectEvents.ProtocolError             => true,
                        _ => false
                    };

                    if (serverDown)
                    {
                        Debug.Log("서버의 연결이 끊어져 자신의 승리로 처리함.");
                        FinishGameLocal(false, NetworkManager.LocalClientId);
                    }
                }
            }
        }
    }
}
