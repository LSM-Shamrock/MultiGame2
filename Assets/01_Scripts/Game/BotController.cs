using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class BotController : MonoBehaviour
{
    [SerializeField, ComponentField] private Player Player;
    [SerializeField, ChildrenArrayField] private Transform[] AutoSummonPositions;

    private float _mp;
    private int[] _handCardIds = new int[4];
    private int _selectedIndex = -1;
    private CardData _selectedCard;

    private void Start()
    {
        Player.MP.OnValueChanged += OnMpChanged;
        Player.HandCardIds.OnListChanged += OnHandCardIdChanged;

        _mp = Player.MP.Value;
        _handCardIds = Player.HandCardIds.AsNativeArray().ToArray();
    }
    private void Update()
    {
        if (!Player.IsSpawned) return;
        if (!Player.IsBot) return;

        Debug.Log("!", Player);

        if (_selectedIndex < 0 || _selectedIndex >= _handCardIds.Length)
        {
            var index = Random.Range(0, _handCardIds.Length);
            var cardId = _handCardIds[index];

            _selectedIndex = index;
            _selectedCard = RemoteConfigManager.Instance.GameData.Value.CardData.Dictionary[cardId];
        }

        if (_mp >= _selectedCard.CostMP)
        {
            int posIndex = Random.Range(0, AutoSummonPositions.Length);
            Vector2 pos = AutoSummonPositions[posIndex].position;
            int gridIndex = Player.WorldToGridIndex(pos);
            Player.SummonCard(_selectedIndex, gridIndex);
        }
    }

    private void OnMpChanged(int oldValue, int newValue)
    {
        _mp = newValue;
    }
    private void OnHandCardIdChanged(NetworkListEvent<int> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<int>.EventType.Add:
            case NetworkListEvent<int>.EventType.Value:
                _handCardIds[changeEvent.Index] = changeEvent.Value;
                _selectedIndex = -1;
                break;
        }
    }
}
