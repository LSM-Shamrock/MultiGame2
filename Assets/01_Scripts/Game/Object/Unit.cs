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
    public const float GROUND_Y = -2.5f;

    public override Collider2D Collider => _collider;

    public NetworkVariable<int> UnitId { get; set; } = new();

    [SerializeField, ChildField("AnimationPoint")] private Transform _animationPoint;
    [SerializeField, ChildField("UnitSprite")] private SpriteRenderer _unitSprite;
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

        StartCoroutine(Routine());
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
        _unitSprite.sprite = sprite;
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
    private IEnumerator Routine()
    {
        var moveRoutine = GetMoveRoutine();

        while (true)
        {
            yield return null;

            FindTarget(out _target, out float horizontalDistance);

            float xDir = _target.transform.position.x - transform.position.x;
            xDir = xDir == 0 ? 0 : xDir / Mathf.Abs(xDir);
            transform.right = Vector3.right * xDir;

            float attackDistance = _unitData.AttackRangeType switch
            {
                AttackRangeType.Horizontal => horizontalDistance,
                AttackRangeType.Directional => GetColliderDistance(_target),
                _ => horizontalDistance
            };

            if (attackDistance > _unitData.AttackRange)
            {
                if (moveRoutine.MoveNext() == false)
                {
                    moveRoutine = GetMoveRoutine();
                    moveRoutine.MoveNext();
                }
                yield return moveRoutine.Current;
            }
            else
            {
                moveRoutine = GetMoveRoutine();
                
                yield return GetAttackRoutine();
            }
        }
    }
    private IEnumerator GetMoveRoutine()
    {
        return _unitData.MoveType switch
        {
            MoveType.Directional => Move_Directional(StaticDB.Instance.Move_DirectionalData.Dictionary.GetValueOrDefault(_unitData.MoveId)),
            MoveType.HorizontalAndFall => Move_HorizontalAndFall(StaticDB.Instance.Move_HorizontalAndFallData.Dictionary.GetValueOrDefault(_unitData.MoveId)),
            MoveType.HorizontalAndUpDown => Move_HorizontalAndUpDown(StaticDB.Instance.Move_HorizontalAndUpDownData.Dictionary.GetValueOrDefault(_unitData.MoveId)),
            _ => null
        };
    }
    private IEnumerator GetAttackRoutine()
    {
        return _unitData.AttackType switch
        {
            AttackType.Motion => Attack_Body(),
            AttackType.Projectile => Attack_Projectile(),
            _ => null
        };
    }
    private IEnumerator Move_Directional(Move_DirectionalData data)
    {
        _unitAnimator.Play(data.Animation);
        transform.position += (_target.ColliderCenter - ColliderCenter).normalized * Time.deltaTime * data.Speed;
        yield break;
    }
    private IEnumerator Move_HorizontalAndFall(Move_HorizontalAndFallData data)
    {
        _unitAnimator.Play(data.Animation);
        transform.position += transform.right * Time.deltaTime * 1f;
        yield break;

    }
    private IEnumerator Move_HorizontalAndUpDown(Move_HorizontalAndUpDownData data)
    {
        _unitAnimator.Play(data.Animation);
        
        while (transform.position.y < GROUND_Y + data.MaxHeight)
        {
            transform.position += transform.right * Time.deltaTime * data.Speed;
            transform.position += Vector3.up * Time.deltaTime * data.UpDownSpeed;
            yield return null;
        }
        while (transform.position.y > GROUND_Y + data.MinHeight)
        {
            transform.position += transform.right * Time.deltaTime * data.Speed;
            transform.position += Vector3.down * Time.deltaTime * data.UpDownSpeed;
            yield return null;
        }
    }
    private IEnumerator Attack_Body()
    {
        float motionTime = 1f;
        float hitNormalizedTime = 0.4f;
        string clipAndStateName = "Unit_Anim_Attack_Body";
        var clip = _unitAnimator.runtimeAnimatorController.animationClips.First(c => c.name == clipAndStateName);

        _animationPoint.right = _target.transform.position - transform.position;
        _unitSprite.transform.rotation = transform.rotation;
        _unitAnimator.SetFloat("AnimationSpeed", clip.length / motionTime);
        _unitAnimator.Play(clipAndStateName, 0, 0f);

        yield return new WaitForSeconds(motionTime * hitNormalizedTime);

        if (_target)
            _target.TakeHit(_attackHitData);

        yield return new WaitForSeconds(motionTime * (1 - hitNormalizedTime));
    }
    private IEnumerator Attack_Projectile()
    {
        yield break;
    }



    protected override void OnDead()
    {
        base.OnDead();

        if (IsServer)
        {
            NetworkObject.Despawn();
        }
    }
}
