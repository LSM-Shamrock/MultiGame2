using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class BotController : MonoBehaviour
{
    [SerializeField, ComponentField] private Player Player;
    [SerializeField, ChildField("SummonGrid")] private Transform SummonGrid;

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
        UpdateSummon();
    }

    private Vector2Int GetRandomGridPoint()
    {
        Transform grid0 = SummonGrid;
        int i0 = Random.Range(0, grid0.childCount);
        
        Transform grid1 = grid0.GetChild(i0);
        int i1 = Random.Range(0, grid1.childCount);

        Vector2Int result = new Vector2Int(i0, i1);
        return result;
    }
    private void UpdateSummon()
    {
        if (!Player.IsSpawned) return;
        if (!Player.IsBot) return;

        if (_selectedIndex < 0 || _selectedIndex >= _handCardIds.Length)
        {
            var index = Random.Range(0, _handCardIds.Length);
            var cardId = _handCardIds[index];

            _selectedIndex = index;
            _selectedCard = RemoteConfigManager.Instance.GameData.Value.CardData.Dictionary[cardId];
        }

        if (_mp >= _selectedCard.CostMP)
        {
            Vector2Int gridPos = GetRandomGridPoint();
            Player.SummonCard(_selectedIndex, gridPos);
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
