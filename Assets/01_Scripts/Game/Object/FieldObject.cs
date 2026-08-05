using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class FieldObject : NetworkBehaviour
{
    public abstract bool IsKnockbackIgnore { get; }
    public abstract Collider2D GroundCollider { get; }
    public abstract Collider2D BodyCollider { get; }
    public Vector2 BodyColliderCenter => BodyCollider.bounds.center;

    public static bool IsGameFinished => ISceneInstance<GameScene>.SceneInstance.IsGameFinished;
    
    public NetworkVariable<bool> IsDead { get; } = new();
    public NetworkVariable<int> MaxHealth { get; } = new();
    public NetworkVariable<int> CurrentHealth { get; } = new();

    protected Queue<(Vector2 direction, float distance, float speed)> KnockbackQueue = new();
    protected Queue<int> DamageQueue = new();
    protected Queue<int> HealQueue = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        IsDead.OnValueChanged += OnIsDeadChanged;
    }
    protected virtual void Update()
    {
        while (KnockbackQueue.TryDequeue(out var knockback))
            ApplyKnockback(knockback.direction, knockback.distance, knockback.speed);

        while (DamageQueue.TryDequeue(out int damage))
            ApplyDamage(damage);
    }
    protected virtual void LateUpdate()
    {
        while (HealQueue.TryDequeue(out int amount))
            ApplyHeal(amount);
    }

    public float GetGroundColliderDistance(FieldObject target)
    {
        Collider2D a = GroundCollider;
        Collider2D b = target?.GroundCollider;
        if (a == null || b == null)
            return float.PositiveInfinity;

        float distance = Physics2D.Distance(a, b).distance;
        return distance;
    }
    public float GetBodyColliderDistance(FieldObject target)
    {
        Collider2D a = BodyCollider;
        Collider2D b = target?.BodyCollider;
        if (a == null || b == null)
            return float.PositiveInfinity;

        float distance = Physics2D.Distance(a, b).distance;
        return distance;
    }

    public static void ApplyHit(FieldObject target, FieldObject attacker, AttackHitData data, Vector2 hitDirection)
    {
        if (IsGameFinished) return;
        if (target.IsDead.Value) return;

        target.DamageQueue.Enqueue(data.Damage);
        target.KnockbackQueue.Enqueue((hitDirection.normalized, data.KnockbackDistance, data.KnockbackSpeed));

        attacker.HealQueue.Enqueue((int)(data.Damage * data.DrainRatio));

        if (string.IsNullOrEmpty(data.EffectAnimation) == false)
        {
            ISceneInstance<EffectPool>.SceneInstance.ShowHitEffectRpc(data.AttackHitId, target.BodyColliderCenter);
        }

        if (RemoteConfigManager.Instance.GameData.Value.DotEffectData.Dictionary.TryGetValue(data.DotEffectId, out var dotEffectData))
        {
            target.ApplyDotEffect(dotEffectData);
        }
    }

    protected virtual void ApplyDamage(int damage)
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
            ApplyDead();
        }
    }
    protected virtual void ApplyHeal(int amount)
    {
        if (IsDead.Value)
            return;

        if (CurrentHealth.Value + amount > MaxHealth.Value)
            CurrentHealth.Value = MaxHealth.Value;
        else
            CurrentHealth.Value += amount;
    }
    protected void ApplyDead()
    {
        if (IsDead.Value)
            return;

        IsDead.Value = true;
    }

    private void ApplyKnockback(Vector2 direction, float distance, float speed)
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

    private void ApplyDotEffect(DotEffectData data)
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

            ApplyDamage(data.DotDamage);
        }
    }

    private void OnIsDeadChanged(bool oldV, bool newV)
    {
        if (oldV == false && newV == true)
            OnDead();
    }
    protected virtual void OnDead()
    {

    }
}
