using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class Unit : NetworkBehaviour
{
    public NetworkVariable<int> CardId = new();

    [SerializeField, ChildField] 
    private SpriteRenderer UnitSprite;

    private int _cardId;
    private CardData _cardData;

    public void Init(int cardId)
    {
        _cardId = cardId;
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
        string path = $"UnitSprites/{_cardData.CodeName}";
        Sprite sprite = Resources.Load<Sprite>(path);

        UnitSprite.sprite = sprite;
        UnitSprite.transform.localPosition = new Vector3(0, _cardData.SummonY);
    }
}
