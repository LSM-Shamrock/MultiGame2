using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using System.Linq;

public class LobbyData : SaveData
{
    [JsonProperty] public string PlayerName;
    [JsonProperty] public int[] DeckCardIds;
}

public class LobbyManager : SingletonBehaviour<LobbyManager>
{
    private readonly LobbyData _data = new();

    public ObservableArray<int> DeckCardIds { get; } = new(8);
    public ObservableValue<string> PlayerName { get; } = new();

    public override void Initialize()
    {
        base.Initialize();

        DeckCardIds.AddListener(OnDeckCardIdsChanged);
        PlayerName.AddListener(OnPlayerNameChanged);

        if (_data.TryLoad() == false)
        {
            _data.DeckCardIds = new int[8];
            for (int i = 0; i < 8; i++)
                _data.DeckCardIds[i] = RemoteConfigManager.Instance.GameData.Value.CardData.List[i].CardId;
            _data.Save();
        }
        PlayerName.Value = _data.PlayerName;
        for (int i = 0; i < 8;i++)
            DeckCardIds[i] = _data.DeckCardIds[i];
    }

    private void OnDeckCardIdsChanged(int index, int cardId)
    {
        if (_data.DeckCardIds[index] != cardId)
        {
            _data.DeckCardIds[index] = cardId;
            _data.Save();
        }
    }
    private void OnPlayerNameChanged(string playerName)
    {
        if (_data.PlayerName != playerName)
        {
            _data.PlayerName = playerName;
            _data.Save();
        }
    }
}
