using UnityEngine;

public class LobbyManager : SingletonBehaviour<LobbyManager>
{
    public ObservableArray<int> CurrentDeckCardIds
    {
        get
        {
            if (_currentDeck == null)
            {
                _currentDeck = new ObservableArray<int>(8);

                for (int i = 0; i < 8; i++)
                    _currentDeck[i] = RemoteConfigManager.Instance.GameData.Value.CardData.List[i].CardId;
            }
            return _currentDeck;
        }
    }
    private ObservableArray<int> _currentDeck;
    public string PlayerName { get; set; }

    private void Awake()
    {
        InitSingleton();
    }
}
