using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Unity.Netcode;
using UnityEngine;


[AutoInjectionTarget]
public class Unit : FieldObject
{
    private const float GROUND_Y = -2.5f;
    private const float X_MIN = -11.5f;
    private const float X_MAX = 11.5f;

    public override Collider2D Collider => _collider;

    public NetworkVariable<int> UnitId { get; set; } = new();

    [SerializeField, ChildField("AnimationPoint")] private Transform _animationPoint;
    [SerializeField, ChildField("UnitSprite")] private SpriteRenderer _unitSpriteRenderer;
    [SerializeField, ChildField("UnitSprite")] private Animator _unitAnimator;
    [SerializeField, ChildField("ColliderNormal")] private Collider2D _colliderNormal;
    [SerializeField, ChildField("ColliderSmall")] private Collider2D _colliderSmall;

    private int _unitId;
    private UnitData _unitData;
    private AttackHitData _attackHitData;
    private Player _owner;
    private Player _opponent;
    private Collider2D _collider;
    private FieldObject _target;
    private Coroutine _attackCoroutine;
    private Coroutine _verticalMoveCoroutine;

    public void Init(int unitId, Player owner, Player opponent)
    {
        _owner = owner;
        _opponent = opponent;

        _unitId = unitId;
        _unitData = StaticDB.Instance.UnitData.Dictionary[_unitId];
        _attackHitData = StaticDB.Instance.AttackHitData.Dictionary[_unitData.AttackHitId];
        _collider = _unitData.ColliderType switch
        {
            ColliderType.Normal => _colliderNormal,
            ColliderType.Small => _colliderSmall,
            _ => _colliderNormal,
        };
        _collider.enabled = true;
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
            if (_unitData.AltitudeType == AltitudeType.Ground)
                _owner.GroundUnits.Add(this);
        }
        else
        {
            _unitId = UnitId.Value;
            _unitData = StaticDB.Instance.UnitData.Dictionary[_unitId];
        }

        string path = $"UnitSprite/{_unitData.CodeName}";
        Sprite sprite = Resources.Load<Sprite>(path);
        _unitSpriteRenderer.sprite = sprite;
        transform.localScale = Vector3.one * _unitData.Scale;
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer)
        {
            _owner.AllUnits.Remove(this);
            _owner.GroundUnits.Remove(this);
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
                AttackRangeType.Directional => GetColliderDistance(_target),
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
        horizontalDistance = GetColliderHorizontalDistance(find);

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
            var dist = GetColliderHorizontalDistance(unit);
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
        if (distance <= _unitData.AttackRange && _attackCoroutine == null)
        {
            var enumerator = _unitData.AttackType switch
            {
                AttackType.Motion => Attack_Motion(target),
                AttackType.Projectile => Attack_Projectile(target),
                _ => null
            };
            if (enumerator != null)
                _attackCoroutine = StartCoroutine(enumerator);
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

    private IEnumerator Attack_Motion(FieldObject target)
    {
        float motionTime = 1f;
        float hitNormalizedTime = 0.4f;
        string clipAndStateName = "Unit_Anim_Attack_Body";
        var clip = _unitAnimator.runtimeAnimatorController.animationClips.First(c => c.name == clipAndStateName);

        _animationPoint.right = target.transform.position - transform.position;
        _unitSpriteRenderer.transform.rotation = transform.rotation;
        _unitAnimator.SetFloat("AnimationSpeed", clip.length / motionTime);
        _unitAnimator.Play(clipAndStateName, 0, 0f);

        yield return new WaitForSeconds(motionTime * hitNormalizedTime);

        if (target)
            target.TakeHit(_attackHitData);

        yield return new WaitForSeconds(motionTime * (1 - hitNormalizedTime));

        _attackCoroutine = null;
    }
    private IEnumerator Attack_Projectile(FieldObject target)
    {
        _attackCoroutine = null;
        yield break;
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
}
