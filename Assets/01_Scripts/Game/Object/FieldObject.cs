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

    public static void ApplyHit(FieldObject target, FieldObject attacker,  AttackHitData data, Vector2 hitDirection)
    {
        if (target.IsDead.Value)
            return;

        target.OnDamage(data.Damage);
        target.OnKnockback(hitDirection.normalized, data.KnockbackDistance, data.KnockbackSpeed);

        attacker.OnHeal((int)(data.Damage * data.DrainRatio));

        if (string.IsNullOrEmpty(data.EffectAnimation) == false)
        {
            HitEffectPool.Instance.ShowHitEffectRpc(data.AttackHitId, target.ColliderCenter);
        }
    }

    protected virtual void OnDamage(int damage)
    {
        CurrentHealth.Value -= damage;

        if (CurrentHealth.Value <= 0)
            OnDead();
    }
    protected virtual void OnKnockback(Vector2 direction, float distance, float speed)
    {

    }
    protected virtual void OnHeal(int amount)
    {
        if (CurrentHealth.Value + amount > MaxHealth.Value)
            CurrentHealth.Value = MaxHealth.Value;
        else
            CurrentHealth.Value += amount;  
    }
    protected virtual void OnDead()
    {
        IsDead.Value = true;
    }
}
