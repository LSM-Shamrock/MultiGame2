using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

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
    private Vector3 _moveDirection;
    private float _currentMoveDistance;
    private Dictionary<FieldObject, float> _pierceHitWaitings = new();


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

        RefreshMoveDirection();
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

    private void DestroyProjectile()
    {
        if (IsSpawned)
            NetworkObject.Despawn();
    }
    private void RefreshMoveDirection()
    {
        Vector3 targetPoint = _projectileData.TargetPoint switch
        {
            ProjectileTargetPoint.TargetCenter => _target.BodyColliderCenter,
            ProjectileTargetPoint.TargetGround => _target.transform.position,
            _ => _target.BodyColliderCenter
        };
        _moveDirection = (targetPoint - transform.position).normalized;


        switch (_projectileData.FacingType)
        {
            case ProjectileFacingType.Rotate:
                transform.right = _moveDirection;
                _spriteRenderer.flipY = _moveDirection.x < 0;
                break;
            case ProjectileFacingType.FlipX:
                _spriteRenderer.flipX = _moveDirection.x < 0;
                Debug.Log(_projectileData.FacingType);
                break;
        }
    }
    private IEnumerable<FieldObject> GetCollisionTargets()
    {
        HashSet<FieldObject> targets = _projectileData.CollisionTarget switch
        {
            ProjectileCollisionTarget.Ground => _unit.Opponent.GroundObjects,
            ProjectileCollisionTarget.GroundOrAir => _unit.Opponent.AllObjects,
            _ => _unit.Opponent.AllObjects,
        };

        return targets;
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

        transform.position += _moveDirection * amount;
        _currentMoveDistance += amount;

        if (_currentMoveDistance > _projectileData.MaxDistance)
            DestroyProjectile();
    }
    private void UpdateCollision()
    {
        var targets = GetCollisionTargets();

        foreach (var obj in targets)
        {
            if (obj == null)
                continue;

            if (obj.BodyCollider.bounds.Intersects(_collider.bounds))
            {
                if (_pierceHitWaitings.TryGetValue(obj, out float waiting) && waiting > 0)
                    continue;

                FieldObject.ApplyHit(obj, _unit, _attackHitData, _moveDirection);

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
}
