using Newtonsoft.Json;
using System;
using UnityEngine;

public class CoreData
{
    public float Scale { get; set; }
    public float ColliderWidth { get; set; }
    public float ColliderHeight { get; set; }
    public int Health { get; set; }
    public CoreData(float scale, float colliderWidth, float colliderHeight, int health)
    {
        Scale = scale;
        ColliderWidth = colliderWidth;
        ColliderHeight = colliderHeight;
        Health = health;
    }
}

[AutoInjectionTarget]
public class Core : FieldObject
{
    public override Collider2D GroundCollider => _groundCollider;
    public override Collider2D BodyCollider => _bodyCollider;
    public override bool IsKnockbackIgnore => true;

    [SerializeField, ChildField("Sprite")] private SpriteRenderer _spriteRenderer;
    [SerializeField, ChildField("Sprite")] private CapsuleCollider2D _bodyCollider;
    [SerializeField, ChildField("Shadow")] private Collider2D _groundCollider;

    private CoreData _coreData;

    public event Action<Core> OnCoreDead;

    public void Init(CoreData coreData)
    {
        _coreData = coreData;
        _bodyCollider.size = new Vector2(_coreData.ColliderWidth, _coreData.ColliderHeight);
        _bodyCollider.offset = new Vector2(0, _bodyCollider.size.y / 2f);
        transform.localScale = Vector3.one * _coreData.Scale;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            MaxHealth.Value = _coreData.Health;
            CurrentHealth.Value = _coreData.Health;
        }

        if (IsOwner)
        {
            _spriteRenderer.sprite = Resources.Load<Sprite>("CoreSprite/Core_Blue");
        }
        else
        {
            _spriteRenderer.sprite = Resources.Load<Sprite>("CoreSprite/Core_Red");
        }

        IsDead.OnValueChanged += OnDead;
    }

    protected override void ApplyDead()
    {
        base.ApplyDead();

        OnCoreDead?.Invoke(this);
    }

    private void OnDead(bool oldValue, bool newValue)
    {
        gameObject.SetActive(false);
    }
}
