using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class Projectile : NetworkBehaviour
{
    [SerializeField, ChildField("ProjectileSprite")] private SpriteRenderer _spriteRenderer;
    [SerializeField, ChildField("ProjectileSprite")] private Animator _animator;
    [SerializeField, ChildField("Collider")] private BoxCollider2D _collider;

    public NetworkVariable<int> ProjectileId { get; } = new();

    private Unit _unit;
    private FieldObject _target;
    private ProjectileData _projectileData;
    private AttackHitData _attackHitData;
    private float _currentMoveDistance;
    private Dictionary<FieldObject, float> _pierceHitWaitings = new();

    private Vector3 _moveDirection;
    private Vector3 MoveDirection
    {
        get => _moveDirection;
        set
        {
            _moveDirection = value;
            transform.right = _moveDirection;
            _spriteRenderer.flipY = _moveDirection.x < 0;
        }
    }

    public void Init(Unit unit, FieldObject target, ProjectileData data)
    {
        _unit = unit;
        _target = target;
        _projectileData = data;
        _attackHitData = RemoteConfigManager.Instance.GameData.Value.AttackHitData.Dictionary[data.AttackHitId];

        _animator.Play($"{data.CodeName}");

        transform.localScale = Vector3.one * data.Scale;
        _collider.size = new Vector2(data.ColliderWidth, data.ColliderHeight);
        _collider.offset = new Vector2(data.ColliderOffsetX, data.ColliderOffsetY);

        MoveDirection = (_target.ColliderCenter - transform.position).normalized;
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            ProjectileId.Value = _projectileData.ProjectileId;
            _spriteRenderer.sortingLayerName = _projectileData.SortingLayerName;
        }
        else
        {
            _projectileData = RemoteConfigManager.Instance.GameData.Value.ProjectileData.Dictionary[ProjectileId.Value];
            _spriteRenderer.sortingLayerName = _projectileData.SortingLayerName;
        }
    }

    private void Update()
    {
        if (IsServer && IsSpawned)
        {
            UpdateMove();
            UpdateCollision();
        }
    }

    private void UpdateMove()
    {
        float amount = _projectileData.Speed * Time.deltaTime;

        transform.position += MoveDirection * amount;
        _currentMoveDistance += amount;

        if (_currentMoveDistance > _projectileData.MaxDistance)
            DestroyProjectile();
    }
    private void UpdateCollision()
    {
        FieldObject[] fieldObjects = new FieldObject[_unit.Opponent.AllObjects.Count];
        _unit.Opponent.AllObjects.CopyTo(fieldObjects);

        foreach (var obj in fieldObjects)
        {
            if (obj == null)
                continue;

            if (obj.Collider.bounds.Intersects(_collider.bounds))
            {
                if (_pierceHitWaitings.TryGetValue(obj, out float waiting) && waiting > 0)
                    continue;

                FieldObject.ApplyHit(obj, _unit, _attackHitData, MoveDirection);

                if (_projectileData.IsPierce)
                    _pierceHitWaitings[obj] = _projectileData.PierceHitInterval;
                else
                    DestroyProjectile();
            }
        }

        FieldObject[] waitings = new FieldObject[_pierceHitWaitings.Count];
        _pierceHitWaitings.Keys.CopyTo(waitings, 0);

        foreach (var obj in waitings)
        {
            if (obj == null) continue;
            if (_pierceHitWaitings[obj] > 0)
                _pierceHitWaitings[obj] -= Time.deltaTime;
        }
    }

    private void DestroyProjectile()
    {
        if (IsSpawned)
            NetworkObject.Despawn();
    }
}
