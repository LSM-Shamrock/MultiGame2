using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;
using Debug = UnityEngine.Debug;

public struct PlayerGameFinishData : INetworkSerializable
{
    private ulong _clientId;
    private int _corePoint;
    private string _playerName;
    
    public ulong ClientId => _clientId;
    public int CorePoint => _corePoint;
    public string PlayerName => _playerName;

    public PlayerGameFinishData(ulong clientId, int corePoint, string playerName)
    {
        _clientId = clientId;
        _corePoint = corePoint;
        _playerName = playerName;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _clientId);
        serializer.SerializeValue(ref _corePoint);
        serializer.SerializeValue(ref _playerName);
    }
}

public struct GameFinishData : INetworkSerializable
{
    private PlayerGameFinishData _playerData1;
    private PlayerGameFinishData _playerData2;

    public PlayerGameFinishData PlayerData1 => _playerData1;
    public PlayerGameFinishData PlayerData2 => _playerData2;

    public GameFinishData(PlayerGameFinishData playerData1, PlayerGameFinishData playerData2)
    {
        _playerData1 = playerData1;
        _playerData2 = playerData2;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _playerData1);
        serializer.SerializeValue(ref _playerData2);
    }
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
    public event Action OnGameFinishedDueToOpponentDisconnect;
    
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

            localPlayer.Opponent = opponentPlayer;
            opponentPlayer.Opponent = localPlayer;
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
        if (IsGameFinished)
            return;

        IsGameFinished = true;
        OnGameFinished?.Invoke(data);
    }
    private void FinishGame()
    {
        Player player1 = LocalPlayer.Value;
        Player player2 = OpponentPlayer.Value;

        var playerData1 = new PlayerGameFinishData(
            clientId: player1.OwnerClientId,
            corePoint: player1.CorePoint,
            playerName: player1.PlayerName.Value.ToString());

        var playerData2 = new PlayerGameFinishData(
            clientId: player2.OwnerClientId,
            corePoint: player2.CorePoint,
            playerName: player2.PlayerName.Value.ToString());

        var gameFinishData = new GameFinishData(playerData1, playerData2);
        FinishGameRpc(gameFinishData);
    }

    private void FinishGameOnOpponentDisconnect()
    {
        if (IsGameFinished)
            return;

        IsGameFinished = true;
        OnGameFinishedDueToOpponentDisconnect?.Invoke();
    }

    private void OnClientDisconect(ulong clientId)
    {
        if (IsGameFinished)
            return;

        if (IsHost)
        {
            if (clientId == _opponentPlayerSessionData.ClientId)
            {
                Debug.Log("상대 클라이언트의 접속이 끊어져 자신의 승리로 처리함.");
                FinishGameOnOpponentDisconnect();
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
                    FinishGameOnOpponentDisconnect();
                }
            }
        }
    }
    private void CheckCoreDead()
    {
        if (!IsServer)
            return;
        
        if (LocalPlayer.Value != null && OpponentPlayer.Value != null && !IsGameFinished)
        {
            if (LocalPlayer.Value.IsDead || OpponentPlayer.Value.IsDead)
            {
                FinishGame();
            }
        }
    }
    private void CheckTimeout()
    {
        if (IsGameFinished)
            return;

        if (IsServer && RemainingTime <= 0)
        {
            FinishGame();
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
