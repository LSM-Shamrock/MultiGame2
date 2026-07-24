using Newtonsoft.Json;
using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public enum GameFinishType
{
    CoreDestroyed,
    ClientDisconected,
}

public struct GameFinishData : INetworkSerializable
{
    private GameFinishType _gameFinishType;
    private bool _isDraw;
    private ulong _winnerClientId;

    public GameFinishType GameFinishType => _gameFinishType;
    public ulong? WinnerClientId => _isDraw ? null : _winnerClientId;

    public GameFinishData(GameFinishType gameFinishType, ulong? winnerClientId)
    {
        _gameFinishType = gameFinishType;
        _isDraw = winnerClientId == null;
        _winnerClientId = winnerClientId ?? 0;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _gameFinishType);
        serializer.SerializeValue(ref _isDraw);
        serializer.SerializeValue(ref _winnerClientId);
    }
}

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
    public event Action<GameFinishData> OnGameFinished;

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
                if (LocalPlayer.Value.IsDead && OpponentPlayer.Value.IsDead)
                {
                    FinishGameRpc(new GameFinishData(
                        gameFinishType: GameFinishType.CoreDestroyed,
                        winnerClientId: null));
                }
                else if (LocalPlayer.Value.IsDead)
                {
                    FinishGameRpc(new GameFinishData(
                        gameFinishType: GameFinishType.CoreDestroyed,
                        winnerClientId: OpponentPlayer.Value.OwnerClientId));
                }
                else if (OpponentPlayer.Value.IsDead)
                {
                    FinishGameRpc(new GameFinishData(
                        gameFinishType: GameFinishType.CoreDestroyed,
                        winnerClientId: LocalPlayer.Value.OwnerClientId));
                }
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
    private void FinishGameRpc(GameFinishData data)
    {
        FinishGameLocal(data);
    }
    private void FinishGameLocal(GameFinishData data)
    {
        if (IsGameFinished)
            return;

        IsGameFinished = true;
        OnGameFinished?.Invoke(data);
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
                    FinishGameLocal(new GameFinishData(
                        gameFinishType: GameFinishType.ClientDisconected, 
                        winnerClientId: NetworkManager.LocalClientId));
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
                        FinishGameLocal(new GameFinishData(
                            gameFinishType: GameFinishType.ClientDisconected,
                            winnerClientId: NetworkManager.LocalClientId));
                    }
                }
            }
        }
    }
}
