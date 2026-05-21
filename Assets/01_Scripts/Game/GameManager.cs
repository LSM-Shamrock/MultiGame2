using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance => _instance ?? (_instance = FindAnyObjectByType<GameManager>());

    public PlayerNumber LocalPlayerNumber { get; private set; }
    public PlayerNumber OtherPlayerNumber { get; private set; }

    private Dictionary<PlayerNumber, PlayerSessionData> _playerSessionDatas = new();
    public IReadOnlyDictionary<PlayerNumber, PlayerSessionData> PlayerSessionDatas => _playerSessionDatas;

    private void Start()
    {
        _instance = this;

        foreach (var (clientId, data) in LobbyManager.Instance.PlayerSessionDatas)
        {
            bool isLocal = clientId == NetworkManager.Singleton.LocalClientId;
            if (isLocal)
                LocalPlayerNumber = data.PlayerNumber;
            else
                OtherPlayerNumber = data.PlayerNumber;

            _playerSessionDatas[data.PlayerNumber] = data;
        }
    }
}
