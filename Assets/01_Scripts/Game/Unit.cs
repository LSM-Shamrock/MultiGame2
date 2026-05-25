using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class Unit : FieldObject
{
    public override Collider2D Collider => _collider;

    public NetworkVariable<int> CardId { get; set; } = new();

    [SerializeField, ComponentField] private SpriteRenderer _spriteRenderer;
    [SerializeField, ChildField] private Collider2D _colliderNormal;
    [SerializeField, ChildField] private Collider2D _colliderSmall;

    private int _cardId;
    private CardData _cardData;
    private Player _owner;
    private Player _opponent;
    private Collider2D _collider;

    public void Init(int cardId, Player owner, Player opponent)
    {
        _owner = owner;
        _opponent = opponent;

        _cardId = cardId;
        _cardData = StaticDB.Instance.CardDataTable[_cardId];
        _collider = _cardData.ColliderType switch
        {
            ColliderType.Normal => _colliderNormal,
            ColliderType.Small => _colliderSmall,
            _ => _colliderNormal,
        };
        _collider.enabled = true;
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
            _cardData = StaticDB.Instance.CardDataTable[_cardId];
        }

        string path = $"UnitSprite/Unit_{_cardData.CodeName}";
        Sprite sprite = Resources.Load<Sprite>(path);

        _spriteRenderer.sprite = sprite;
    }
}
