using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class Unit : FieldObject
{
    public NetworkVariable<int> CardId { get; set; } = new();

    private const float GROUND_Y = -2.5f;

    [SerializeField, ComponentField] private SpriteRenderer SpriteRenderer;
    [SerializeField, ChildField] private Transform Shadow;

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

        SpriteRenderer.sprite = sprite;
    }

    private void LateUpdate()
    {
        Shadow.position = new Vector3(Shadow.position.x, GROUND_Y);
    }
}
