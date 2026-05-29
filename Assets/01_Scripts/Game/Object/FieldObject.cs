using Unity.Netcode;
using UnityEngine;

public abstract class FieldObject : NetworkBehaviour
{
    public abstract Collider2D Collider { get; }
    public Vector3 ColliderCenter => Collider.bounds.center;

    public NetworkVariable<bool> IsDead { get; } = new();
    public NetworkVariable<int> MaxHealth { get; } = new();
    public NetworkVariable<int> CurrentHealth { get; } = new();

    public float GetColliderDistance(Collider2D targetCollider)
    {
        if (Collider == null || targetCollider == null)
            return float.PositiveInfinity;

        float result = Physics2D.Distance(Collider, targetCollider).distance;

        return result;
    }
    public float GetColliderHorizontalDistance(Collider2D targetCollider)
    {
        if (Collider == null || targetCollider == null)
            return float.PositiveInfinity;

        var a = Collider.bounds;
        var b = targetCollider.bounds;

        float distanceX = Mathf.Abs(a.center.x - b.center.x) - (a.extents.x + b.extents.x);

        distanceX = Mathf.Max(distanceX, 0f);

        return distanceX;
    }
    public float GetColliderVerticalDistance(Collider2D targetCollider)
    {
        if (Collider == null || targetCollider == null)
            return float.PositiveInfinity;

        var a = Collider.bounds;
        var b = targetCollider.bounds;

        float distanceY = Mathf.Abs(a.center.y - b.center.y) - (a.extents.y + b.extents.y);

        distanceY = Mathf.Max(distanceY, 0f);

        return distanceY;
    }

    public void TakeHit(AttackHitData data)
    {
        if (IsDead.Value)
            return;

        OnDamage(data.Damage);
        OnKnockback(data.Knockback);
    }
    protected virtual void OnDamage(int damage)
    {
        CurrentHealth.Value -= damage;

        if (CurrentHealth.Value <= 0)
            OnDead();
    }
    protected virtual void OnKnockback(float knockback)
    {

    }
    protected virtual void OnDead()
    {
        IsDead.Value = true;
    }
}
