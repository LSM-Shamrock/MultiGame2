using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class Core : FieldObject
{
    public override Collider2D Collider => _collider;
    public override bool IsKnockbackIgnore => true;

    [SerializeField, ComponentField] private SpriteRenderer _spriteRenderer;
    [SerializeField, ComponentField] private Collider2D _collider;
    
    private Player _owner;

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

    protected override void OnDead()
    {
        base.OnDead();

        _owner.IsDead = true;
    }
}
