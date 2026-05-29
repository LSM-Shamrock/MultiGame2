using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;


[AutoInjectionTarget]
public class Unit : FieldObject
{
    private const float GROUND_Y = -2.5f;
    private const float X_MIN = -11.5f;
    private const float X_MAX = 11.5f;

    public override Collider2D Collider => _collider;
    public Player Owner => _owner;
    public Player Opponent => _opponent;

    public NetworkVariable<int> UnitId { get; } = new();

    [SerializeField, ChildField("AnimationPoint")] private Transform _animationPoint;
    [SerializeField, ChildField("UnitSprite")] private SpriteRenderer _unitSpriteRenderer;
    [SerializeField, ChildField("UnitSprite")] private Animator _unitAnimator;
    [SerializeField, ChildField("Collider")] private BoxCollider2D _collider;
    [SerializeField, AssetField("Projectile")] private GameObject _projectilePrefab;

    private int _unitId;
    private Player _owner;
    private Player _opponent;
    private UnitData _unitData;
    private FieldObject _target;
    private Coroutine _attackCoroutine;
    private Coroutine _verticalMoveCoroutine;
    private float _attackCooltime;

    public void Init(int unitId, Player owner, Player opponent)
    {
        _owner = owner;
        _opponent = opponent;

        _unitId = unitId;
        _unitData = StaticDB.Instance.UnitData.Dictionary[_unitId];
        _collider.size = new Vector2(_unitData.ColliderWidth, _unitData.ColliderHeight);
        _collider.offset = new Vector2(0, _unitData.ColliderHeight / 2f);
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            UnitId.Value = _unitId;
            MaxHealth.Value = _unitData.Health;
            CurrentHealth.Value = _unitData.Health;

            _owner.AllUnits.Add(this);
            _owner.AllObjects.Add(this);

            if (_unitData.AltitudeType == AltitudeType.Ground)
                _owner.GroundUnits.Add(this);

            _unitAnimator.Play($"{_unitData.CodeName}");
        }
        else
        {
            _unitId = UnitId.Value;
            _unitData = StaticDB.Instance.UnitData.Dictionary[_unitId];
        }

        transform.localScale = Vector3.one * _unitData.Scale;
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer)
        {
            _owner.AllUnits.Remove(this);
            _owner.GroundUnits.Remove(this);
            _owner.AllObjects.Remove(this);
        }
    }
    protected override void OnDead()
    {
        base.OnDead();

        if (IsServer)
        {
            NetworkObject.Despawn();
        }
    }
    private void Update()
    {
        if (IsServer)
        {
            FindTarget(out _target, out float horizontalDistance);

            float attackDistance = _unitData.AttackRangeType switch
            {
                AttackRangeType.Horizontal => horizontalDistance,
                AttackRangeType.Directional => GetColliderDistance(_target.Collider),
                _ => horizontalDistance
            };
            UpdateMove(_target, attackDistance);
            UpdateAttack(_target, attackDistance);
            UpdateVerticalMove();
        }
    }
    private void LateUpdate()
    {
        if (IsServer)
        {
            if (transform.position.x > X_MAX)
                transform.position = new Vector3(X_MAX, transform.position.y);
            if (transform.position.x < X_MIN)
                transform.position = new Vector3(X_MIN, transform.position.y);
        }
    }

    private void FindTarget(out FieldObject find, out float horizontalDistance)
    {
        find = _opponent.Core;
        horizontalDistance = GetColliderHorizontalDistance(find.Collider);

        if (_unitData.TargetingType == TargetingType.Core)
            return;

        HashSet<Unit> units = _unitData.TargetingType switch
        {
            TargetingType.Ground => _opponent.GroundUnits,
            TargetingType.GroundOrAir => _opponent.AllUnits,
            _ => null
        };

        foreach (Unit unit in units)
        {
            var dist = GetColliderHorizontalDistance(unit.Collider);
            if (dist < horizontalDistance)
            {
                horizontalDistance = dist;
                find = unit;
            }
        }
    }

    private void UpdateMove(FieldObject target, float distance)
    {
        if (_attackCoroutine != null)
            return;

        float xDir = target.transform.position.x - transform.position.x;
        xDir = xDir == 0 ? 0 : xDir / Mathf.Abs(xDir);
        transform.right = Vector3.right * xDir;

        Vector3 dir = _unitData.MoveType switch
        {
            MoveType.Directional => (target.ColliderCenter - ColliderCenter).normalized,
            MoveType.Horizontal => Vector3.right * xDir,
            _ => default,
        };

        if (distance > _unitData.AttackRange)
        {
            _unitAnimator.Play(_unitData.MoveAnimation);
            transform.position += dir * Time.deltaTime * _unitData.MoveSpeed;
        }
        else if (distance < _unitData.AttackRange * _unitData.BackoffRatio)
        {
            _unitAnimator.Play(_unitData.MoveAnimation);
            transform.position -= dir * Time.deltaTime * _unitData.MoveSpeed * _unitData.BackoffSpeedRatio;
        }
    }
    private void UpdateAttack(FieldObject target, float distance)
    {
        if (_attackCoroutine != null)
            return;

        if (_attackCooltime > 0f)
        {
            _attackCooltime -= Time.deltaTime;
        }
        else
        {
            _attackCooltime = 0f;

            if (distance <= _unitData.AttackRange)
            {
                var enumerator = _unitData.AttackType switch
                {
                    AttackType.Motion => Attack_Motion(target, StaticDB.Instance.Attack_MotionData.Dictionary[_unitData.AttackId]),
                    AttackType.Projectile => Attack_Projectile(target, StaticDB.Instance.Attack_ProjectileData.Dictionary[_unitData.AttackId]),
                    _ => null
                };
                if (enumerator != null)
                    _attackCoroutine = StartCoroutine(enumerator);
            }
        }
    }
    private void UpdateVerticalMove()
    {
        if (_verticalMoveCoroutine == null)
        {
            var enumerator = _unitData.VerticalMoveType switch
            {
                VerticalMoveType.Fall => VerticalMove_Fall(StaticDB.Instance.VerticalMove_FallData.Dictionary[_unitData.VerticalMoveId]),
                VerticalMoveType.UpDown => VerticalMove_UpDown(StaticDB.Instance.VerticalMove_UpDownData.Dictionary[_unitData.VerticalMoveId]),
                _ => null
            };
            if (enumerator != null)
                _verticalMoveCoroutine = StartCoroutine(enumerator);
        }
    }

    private IEnumerator Attack_Motion(FieldObject target, Attack_MotionData data)
    {
        var clip = _unitAnimator.runtimeAnimatorController.animationClips.First(c => c.name == data.MotionAnimation);

        _animationPoint.right = target.transform.position - transform.position;
        _unitSpriteRenderer.transform.rotation = transform.rotation;
        _unitAnimator.SetFloat("AnimationSpeed", clip.length / data.MotionTime);
        _unitAnimator.Play(data.MotionAnimation, 0, 0f);

        yield return new WaitForSeconds(data.MotionTime * data.HitNomalizedTime);

        if (target)
            target.TakeHit(StaticDB.Instance.AttackHitData.Dictionary[data.AttackHitId]);

        yield return new WaitForSeconds(data.MotionTime * (1 - data.HitNomalizedTime));

        _attackCooltime = data.Cooltime;
        _attackCoroutine = null;
    }
    private IEnumerator Attack_Projectile(FieldObject target, Attack_ProjectileData data)
    {
        var clip = _unitAnimator.runtimeAnimatorController.animationClips.First(c => c.name == data.MotionAnimation);

        _unitAnimator.Play(data.MotionAnimation, 0, 0f);

        if (target)
            SummonProjectile(target, StaticDB.Instance.ProjectileData.Dictionary[data.ProjectileId]);

        yield return new WaitForSeconds(clip.length);
        
        _attackCooltime = data.Cooltime;
        _attackCoroutine = null;
        _unitAnimator.Play("Unit_Anim_None", 0, 0f);
    }

    private IEnumerator VerticalMove_Fall(VerticalMove_FallData data)
    {
        float amount = Time.deltaTime * data.FallSpeed;

        if (transform.position.y - amount > GROUND_Y)
            transform.position += Vector3.down * amount;
        else
            transform.position = new Vector3(transform.position.x, GROUND_Y);

        _verticalMoveCoroutine = null;
        yield break;
    }
    private IEnumerator VerticalMove_UpDown(VerticalMove_UpDownData data)
    {
        while (transform.position.y < GROUND_Y + data.UpHeight)
        {
            yield return null;

            if (_attackCoroutine == null)
                transform.position += Vector3.up * Time.deltaTime * data.UpSpeed;
        }
        while (transform.position.y > GROUND_Y + data.DownHeight)
        {
            yield return null;

            if (_attackCoroutine == null)
                transform.position += Vector3.down * Time.deltaTime * data.DownSpeed;
        }
        _verticalMoveCoroutine = null;
    }

    private void SummonProjectile(FieldObject target, ProjectileData data)
    {
        Vector3 position = data.SummonPositionType switch
        {
            ProjectileSummonPositionType.UnitCenter => ColliderCenter,
            ProjectileSummonPositionType.UnitGround => new Vector3(transform.position.x, GROUND_Y),
            _ => ColliderCenter,
        };

        GameObject go = Instantiate(_projectilePrefab, position, transform.rotation);
        Projectile projectile = go.GetComponent<Projectile>();
        projectile.Init(this, target, data);
        projectile.NetworkObject.SpawnWithOwnership(OwnerClientId);
    }
}
