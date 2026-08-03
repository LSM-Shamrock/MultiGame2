using System;
using UnityEngine;

[AutoInjectionTarget]
public class Core : FieldObject
{
    public override Collider2D GroundCollider => _groundCollider;
    public override Collider2D BodyCollider => _bodyCollider;
    public override bool IsKnockbackIgnore => true;

    [SerializeField, ChildField("Sprite")] private SpriteRenderer _spriteRenderer;
    [SerializeField, ChildField("Sprite")] private Collider2D _bodyCollider;
    [SerializeField, ChildField("Shadow")] private Collider2D _groundCollider;

    private Player _owner;

    public event Action<Core> OnCoreDead;

    public void Init(Player owner)
    {
        _owner = owner;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            MaxHealth.Value = 5000;
            CurrentHealth.Value = 5000;
        }

        if (IsOwner)
        {
            _spriteRenderer.sprite = Resources.Load<Sprite>("CoreSprite/Core_Blue");
        }
        else
        {
            _spriteRenderer.sprite = Resources.Load<Sprite>("CoreSprite/Core_Red");
        }
    }

    protected override void ApplyDead()
    {
        base.ApplyDead();

        OnCoreDead?.Invoke(this);
    }
}
