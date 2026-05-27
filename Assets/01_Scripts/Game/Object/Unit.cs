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
        _unitData = StaticDB.Instance.UnitDataTable[_unitId];
        _attackHitData = StaticDB.Instance.AttackHitDataTable[_unitData.AttackHitId];
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
            _unitData = StaticDB.Instance.UnitDataTable[_unitId];
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
                switch (_unitData.MoveType)
                {
                    case MoveType.Horizontal: Move_Horizontal(); break;
                    case MoveType.Directional: Move_Directional(); break;
                };
            }
            else
            {
                switch (_unitData.AttackType)
                {
                    case AttackType.Motion: yield return Attack_Motion(); break;
                    case AttackType.Projectile: yield return Attack_Projectile(); break;
                }
            }
        }
    }
    private void Move_Horizontal()
    {
        transform.position += transform.right * Time.deltaTime * 1f;
        _unitAnimator.Play("Unit_Anim_None");
    }
    private void Move_Directional()
    {
        transform.position += (_target.ColliderCenter - ColliderCenter).normalized * Time.deltaTime * 1f;
        _unitAnimator.Play("Unit_Anim_None");
    }
    private IEnumerator Attack_Motion()
    {
        float motionTime = 1f;
        float hitNormalizedTime = 0.4f;
        string clipAndStateName = "Unit_Anim_BodyAttack";
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
