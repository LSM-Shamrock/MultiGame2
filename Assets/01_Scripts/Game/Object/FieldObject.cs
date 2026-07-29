using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class FieldObject : NetworkBehaviour
{
    public abstract Collider2D Collider { get; }
    public Vector3 ColliderCenter => Collider.bounds.center;
    public abstract bool IsKnockbackIgnore { get; }

    public NetworkVariable<bool> IsDead { get; } = new();
    public NetworkVariable<int> MaxHealth { get; } = new();
    public NetworkVariable<int> CurrentHealth { get; } = new();

    protected Queue<(Vector2 direction, float distance, float speed)> KnockbackQueue = new();
    protected Queue<int> DamageQueue = new();
    protected Queue<int> HealQueue = new();

    protected virtual void Update()
    {
        while (KnockbackQueue.TryDequeue(out var knockback))
            OnKnockback(knockback.direction, knockback.distance, knockback.speed);

        while (DamageQueue.TryDequeue(out int damage))
            OnDamage(damage);
    }
    protected virtual void LateUpdate()
    {
        while (HealQueue.TryDequeue(out int amount))
            OnHeal(amount);
    }

    public float GetGroundDistance(FieldObject target)
    {
        if (target == null)
            return float.PositiveInfinity;

        float distance = (target.transform.position - transform.position).magnitude;
        return distance;
    }
    public float GetColliderDistance(FieldObject target)
    {
        if (Collider == null || target?.Collider == null)
            return float.PositiveInfinity;

        float distance = Physics2D.Distance(Collider, target.Collider).distance;
        return distance;
    }

    public static void ApplyHit(FieldObject target, FieldObject attacker, AttackHitData data, Vector2 hitDirection)
    {
        if (target.IsDead.Value)
            return;

        target.DamageQueue.Enqueue(data.Damage);
        target.KnockbackQueue.Enqueue((hitDirection.normalized, data.KnockbackDistance, data.KnockbackSpeed));

        attacker.HealQueue.Enqueue((int)(data.Damage * data.DrainRatio));

        if (string.IsNullOrEmpty(data.EffectAnimation) == false)
        {
            ISceneInstance<EffectPool>.SceneInstance.ShowHitEffectRpc(data.AttackHitId, target.ColliderCenter);
        }

        if (RemoteConfigManager.Instance.GameData.Value.DotEffectData.Dictionary.TryGetValue(data.DotEffectId, out var dotEffectData))
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

        ISceneInstance<EffectPool>.SceneInstance.ShowDotEffectRpc(NetworkObjectId, data.DotEffectId);
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
