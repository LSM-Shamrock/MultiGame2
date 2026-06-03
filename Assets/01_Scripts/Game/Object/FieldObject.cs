using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public abstract class FieldObject : NetworkBehaviour
{
    public abstract Collider2D Collider { get; }
    public Vector3 ColliderCenter => Collider.bounds.center;
    public abstract bool IsKnockbackIgnore { get; }

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
            EffectPool.SceneInstance.ShowHitEffectRpc(data.AttackHitId, target.ColliderCenter);
        }

        if (StaticDB.Instance.DotEffectData.Dictionary.TryGetValue(data.DotEffectId, out var dotEffectData))
        {
            target.OnDotEffect(dotEffectData);
        }
    }

    protected virtual void OnDamage(int damage)
    {
        if (IsDead.Value)
            return;

        if (CurrentHealth.Value > damage)
        {
            CurrentHealth.Value -= damage;
        }
        else
        {
            CurrentHealth.Value = 0;
            OnDead();
        }
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
    
    private void OnKnockback(Vector2 direction, float distance, float speed)
    {
        if (IsKnockbackIgnore == false)
            StartCoroutine(Knockback(direction, distance, speed));
    }
    private IEnumerator Knockback(Vector2 direction, float distance, float speed)
    {
        float accumulated = 0f;

        while (accumulated < distance)
        {
            yield return null;

            float amount = Time.deltaTime * speed;

            transform.position += (Vector3)direction * amount;
            accumulated += amount;
        }
    }

    private void OnDotEffect(DotEffectData data)
    {
        StartCoroutine(DotEffect(data));

        EffectPool.SceneInstance.ShowDotEffectRpc(NetworkObjectId, data.DotEffectId);
    }
    private IEnumerator DotEffect(DotEffectData data)
    {
        var waitForInterval = new WaitForSeconds(data.DotInterval);

        for (int i = 0; i < data.DotCount; i++)
        {
            yield return waitForInterval;

            OnDamage(data.DotDamage);
        }
    }
}
