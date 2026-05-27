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
        _collider = _unitData.ColliderType switch
        {
            ColliderType.Normal => _colliderNormal,
            ColliderType.Small => _colliderSmall,
            _ => _colliderNormal,
        };
        _collider.enabled = true;

        Debug.Log("!");
        StartCoroutine(Routine());
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            UnitId.Value = _unitId;
            MaxHealth.Value = _unitData.Health;
            CurrentHealth.Value = _unitData.Health;
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

    private void FindNearestTarget(out FieldObject find, out float distance)
    {
        find = _opponent.Core;
        distance = GetDistance(find);

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
            var dist = GetDistance(unit);
            if (dist < distance)
            {
                distance = dist;
                find = unit;
            }
        }
    }
    private void FindNearestHorizontalTarget(out FieldObject find, out float distance)
    {
        find = _opponent.Core;
        distance = GetHorizontalDistance(find);

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
            var dist = GetHorizontalDistance(unit);
            if (dist < distance)
            {
                distance = dist;
                find = unit;
            }
        }
    }

    private IEnumerator Routine()
    {
        while (true)
        {
            yield return null;

            FindNearestHorizontalTarget(out _target, out float distance);

            float xDir = _target.transform.position.x - transform.position.x;
            xDir = xDir == 0 ? 0 : xDir / Mathf.Abs(xDir);

            transform.right = Vector3.right * xDir;

            if (distance > 0.1)
            {
                transform.position += Vector3.right * xDir * Time.deltaTime * 1f;
            }
            else
            {
                float animationDuration = 1f;
                float hitNormalizedTime = 0.4f;
                string clipAndStateName = "Unit_Anim_BodyAttack";
                var clip = _unitAnimator.runtimeAnimatorController.animationClips.First(c => c.name == clipAndStateName);
                _unitAnimator.speed = clip.length / animationDuration;
                _unitAnimator.Play(clipAndStateName, 0, 0f);

                yield return new WaitForSeconds(animationDuration * hitNormalizedTime);

                _target.TakeHit(50);

                yield return new WaitForSeconds(animationDuration * (1 - hitNormalizedTime));
            }
        }
    }
}
