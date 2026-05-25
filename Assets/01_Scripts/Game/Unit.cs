using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class Unit : FieldObject
{
    public NetworkVariable<int> CardId { get; set; } = new();

    private int _cardId;
    private CardData _cardData;
    private Player _owner;
    private Player _opponent;

    public void Init(int cardId, Player owner, Player opponent)
    {
        _cardId = cardId;
        _owner = owner;
        _opponent = opponent;
    }
    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            CardId.Value = _cardId;
        }
        else
        {
            _cardId = CardId.Value;
        }

        _cardData = StaticDB.Instance.CardDataTable[_cardId];
        string path = $"UnitSprite/Unit_{_cardData.CodeName}";
        Sprite sprite = Resources.Load<Sprite>(path);

        SpriteRenderer.sprite = sprite;
    }
}
