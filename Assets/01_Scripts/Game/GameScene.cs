using NavMeshPlus.Components;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using Unity.Netcode;
using UnityEngine;
using Debug = UnityEngine.Debug;

public enum GameFinishType
{
    CoreDestroyed,
    Timeout,
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

public class GameConfig
{
    public const float DEFAULT_MP_REGEN_SPEED = 0.5f;
    public const float GAME_DURATION = 60 * 3;
    public const float MP_DOUBLE_START_TIME = 60 * 1;

    public const float X_MIN = -18;
    public const float X_MAX = 18;
    public const float Y_MIN = -5;
    public const float Y_MAX = 5;
}

[AutoInjectionTarget]
public class GameScene : NetworkBehaviour, ISceneInstance<GameScene>
{
    [SerializeField, AssetField("Player")] private GameObject _playerPrefab;
    [SerializeField, ChildField("RotationRoot")] private Transform _rotationRoot;
    
    public ObservableValue<Player> LocalPlayer { get; private set; } = new();
    public ObservableValue<Player> OpponentPlayer { get; private set; } = new();
    private PlayerSessionData _localPlayerSessionData;
    private PlayerSessionData _opponentPlayerSessionData;
    private List<Player> _players = new();

    public bool IsGameFinished { get; private set; } = false;
    public event Action<GameFinishData> OnGameFinished;
    
    private Stopwatch _timer = new();
    private float _lastMpRegenTime;
    public float GameDuration => GameConfig.GAME_DURATION;
    public float ElapsedTime => (float)_timer.Elapsed.TotalSeconds;
    public float RemainingTime => ElapsedTime < GameDuration ? GameDuration - ElapsedTime : 0f;
    public float MpRegenScale { get; private set; } = 1f;
    public float MpRegenSpeed => GameConfig.DEFAULT_MP_REGEN_SPEED * MpRegenScale;
    public float MpRegenInterval => 1f / MpRegenSpeed;

    private void Start()
    {
        ((ISceneInstance<GameScene>)this).InitSceneInstance();

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconect;

        SetupPlayer();
        
        _timer.Start();
        _lastMpRegenTime = ElapsedTime;
    }
    public override void OnDestroy()
    {
        base.OnDestroy();

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconect;
    }
    private void Update()
    {
        CheckCoreDead();
        CheckTimeout();
        UpdateMpRegen();
    }

    private Player SpawnPlayer(PlayerSessionData data, bool isRotate)
    {
        GameObject go = Instantiate(_playerPrefab, Vector2.zero, isRotate ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);
        NetworkObject obj = go.GetComponent<NetworkObject>();
        Player player = go.GetComponent<Player>();
        player.Init(data.PlayerName, data.DeckCardIds);
        obj.SpawnAsPlayerObject(data.ClientId);
        
        return player;
    }
    private void SetupPlayer()
    {
        if (NetworkManager.Singleton.IsHost && MatchingManager.Instance)
        {
            _localPlayerSessionData = MatchingManager.Instance.LocalPlayerSessionData;
            _opponentPlayerSessionData = MatchingManager.Instance.OpponentPlayerSessionData;
            var localPlayer = SpawnPlayer(_localPlayerSessionData, false);
            var opponentPlayer = SpawnPlayer(_opponentPlayerSessionData, true);
            opponentPlayer.IsBot = MatchingManager.Instance.MatchingType == MatchingType.PvE;

            _players.Add(localPlayer);
            _players.Add(opponentPlayer);
        }

        if (IsHost == false)
            _rotationRoot.rotation = Quaternion.Euler(0, 180, 0);
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
    private void CheckCoreDead()
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
    private void CheckTimeout()
    {
        if (IsGameFinished)
            return;

        if (IsServer && RemainingTime <= 0)
        {
            FinishGameRpc(new GameFinishData(GameFinishType.Timeout, null));
        }
    }
    private void UpdateMpRegen()
    {
        if (IsGameFinished)
            return;

        if (MpRegenScale < 2f && ElapsedTime > GameConfig.MP_DOUBLE_START_TIME)
        {
            MpRegenScale = 2f;
            SoundManager.Instance.BgmPitch.Value = 1.5f;
        }

        if (ElapsedTime - _lastMpRegenTime > MpRegenInterval)
        {
            _lastMpRegenTime = ElapsedTime;

            foreach (var player in _players)
            {
                if (player.MP.Value < 10) 
                    player.MP.Value++;
            }
        }
    }
}
