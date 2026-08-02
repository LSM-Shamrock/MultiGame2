using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;


[AutoInjectionTarget]
public class Unit : FieldObject
{
    public override Collider2D GroundCollider => _groundCollider;
    public override Collider2D BodyCollider => _bodyCollider;
    public override bool IsKnockbackIgnore => _unitData.IsKnockbackIgnore;
    public Player Owner { get; private set; }
    public Player Opponent { get; private set; }
    public NetworkVariable<int> UnitId { get; } = new();

    [SerializeField, ChildField("UnitSprite")] private SpriteRenderer _unitSpriteRenderer;
    [SerializeField, ChildField("UnitSprite")] private Animator _unitAnimator;
    [SerializeField, ChildField("AnimationDirection")] private Transform _animationDirection;
    [SerializeField, ChildField("HeightPoint")] private Transform _heightPoint;
    [SerializeField, ChildField("HeightPoint")] private BoxCollider2D _bodyCollider;
    [SerializeField, ChildField("Shadow")] private Collider2D _groundCollider;

    [SerializeField, AssetField("Projectile")] 
    private GameObject _projectilePrefab;
    
    private int _unitId;
    private UnitData _unitData;
    private FieldObject _target;
    private Coroutine _attackCoroutine;
    private Coroutine _verticalMoveCoroutine;
    private float _attackCooltime;

    public void Init(int unitId, Player owner, Player opponent)
    {
        Owner = owner;
        Opponent = opponent;

        _unitId = unitId;
        _unitData = RemoteConfigManager.Instance.GameData.Value.UnitData.Dictionary[_unitId];
        _heightPoint.localPosition = new Vector3(0, GameConfig.GetUnitHeight(_unitData.AltitudeType));
        _bodyCollider.size = new Vector2(_unitData.ColliderWidth, _unitData.ColliderHeight);
        _bodyCollider.offset = new Vector2(0, _unitData.ColliderHeight / 2f);
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            UnitId.Value = _unitId;
            MaxHealth.Value = _unitData.Health;
            CurrentHealth.Value = _unitData.Health;

            Owner.AllUnits.Add(this);
            Owner.AllObjects.Add(this);

            if (_unitData.AltitudeType == AltitudeType.Ground)
            {
                Owner.GroundUnits.Add(this);
                Owner.GroundObjects.Add(this);
            }

            _unitAnimator.Play($"{_unitData.CodeName}");
        }
        else
        {
            _unitId = UnitId.Value;
            _unitData = RemoteConfigManager.Instance.GameData.Value.UnitData.Dictionary[_unitId];
        }

        transform.localScale = Vector3.one * _unitData.Scale;
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer)
        {
            Owner.AllUnits.Remove(this);
            Owner.GroundUnits.Remove(this);
            Owner.AllObjects.Remove(this);
        }
    }
    protected override void Update()
    {
        base.Update();

        if (IsServer)
        {
            FindTarget(out _target, out float distance);
            UpdateMove(_target, distance);
            UpdateAttack(_target, distance);
            UpdateVerticalMove();
        }
    }
    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (IsServer)
        {
            Vector3 min = new Vector3(GameConfig.X_MIN, GameConfig.Y_MIN);
            Vector3 max = new Vector3(GameConfig.X_MAX, GameConfig.Y_MAX);
            transform.position = Vector3.Min(transform.position, max);
            transform.position = Vector3.Max(transform.position, min);
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

    private float GetDistance(FieldObject target)
    {
        return GetGroundColliderDistance(target);
    }
    private void FindTarget(out FieldObject find, out float distance)
    {
        find = Opponent.Core;
        distance = GetDistance(find);

        if (_unitData.TargetingType == TargetingType.Core)
            return;

        HashSet<Unit> units = _unitData.TargetingType switch
        {
            TargetingType.Ground => Opponent.GroundUnits,
            TargetingType.GroundOrAir => Opponent.AllUnits,
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

    private Vector2 GetMoveDirection(FieldObject target)
    {
        NavMeshPath path = new NavMeshPath();

        Vector2 a = transform.position;
        Vector2 b = target.transform.position;
        Vector2 result;

        if (NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path) && path.corners.Length > 1)
        {
            result = ((Vector2)path.corners[1] - a).normalized;
        }
        else
        {
            result = Vector2.zero;
        }

        return result;
    }
    private void UpdateMove(FieldObject target, float distance)
    {
        if (_attackCoroutine != null)
            return;

        Vector3 dir = GetMoveDirection(target);
        transform.right = Vector3.right * dir.x;

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

    private void UpdateVerticalMove()
    {
        if (_verticalMoveCoroutine == null)
        {
            var enumerator = _unitData.VerticalMoveType switch
            {
                VerticalMoveType.Fall => VerticalMove_Fall(RemoteConfigManager.Instance.GameData.Value.VerticalMove_FallData.Dictionary[_unitData.VerticalMoveId]),
                VerticalMoveType.UpDown => VerticalMove_UpDown(RemoteConfigManager.Instance.GameData.Value.VerticalMove_UpDownData.Dictionary[_unitData.VerticalMoveId]),
                _ => null
            };
            if (enumerator != null)
                _verticalMoveCoroutine = StartCoroutine(enumerator);
        }
    }
    private IEnumerator VerticalMove_Fall(VerticalMove_FallData data)
    {
        float amount = Time.deltaTime * data.FallSpeed;

        if (_heightPoint.localPosition.y > 0)
            _heightPoint.localPosition += Vector3.down * amount;
        else
            _heightPoint.localPosition = Vector3.zero;

        _verticalMoveCoroutine = null;
        yield break;
    }
    private IEnumerator VerticalMove_UpDown(VerticalMove_UpDownData data)
    {
        float standardHeight = GameConfig.GetUnitHeight(_unitData.AltitudeType);
        float upHeight = standardHeight + data.UpHeight;
        float downHeight = standardHeight - data.DownHeight;
    

        while (_heightPoint.localPosition.y < upHeight)
        {
            yield return null;

            if (_attackCoroutine == null)
                _heightPoint.localPosition += Vector3.up * Time.deltaTime * data.UpSpeed;
        }
        while (_heightPoint.localPosition.y > downHeight)
        {
            yield return null;

            if (_attackCoroutine == null)
                _heightPoint.localPosition += Vector3.down * Time.deltaTime * data.DownSpeed;
        }
        _verticalMoveCoroutine = null;
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
                Attack(target);
            }
        }
    }
    private void Attack(FieldObject target)
    {
        if (_attackCoroutine != null) return;
        if (_attackCooltime > 0f) return;

        IEnumerator enumerator = null;

        switch (_unitData.AttackType)
        {
            case AttackType.Motion:
                Attack_MotionData attack_motionData = RemoteConfigManager.Instance.GameData.Value.Attack_MotionData.Dictionary[_unitData.AttackId];
                enumerator = Attack_Motion(target, attack_motionData);
                break;
            case AttackType.Projectile:
                Attack_ProjectileData attack_projectileData = RemoteConfigManager.Instance.GameData.Value.Attack_ProjectileData.Dictionary[_unitData.AttackId];
                enumerator = Attack_Projectile(target, attack_projectileData);
                break;
        }

        if (enumerator != null)
            _attackCoroutine = StartCoroutine(enumerator);
    }
    private IEnumerator Attack_Motion(FieldObject target, Attack_MotionData data)
    {
        var clip = _unitAnimator.runtimeAnimatorController.animationClips.First(c => c.name == data.MotionAnimation);

        var dir = (target.transform.position - transform.position).normalized;

        _animationDirection.right = dir;
        _unitSpriteRenderer.transform.rotation = transform.rotation;
        _unitAnimator.SetFloat("AnimationSpeed", clip.length / data.MotionTime);
        _unitAnimator.Play(data.MotionAnimation, 0, 0f);

        yield return new WaitForSeconds(data.MotionTime * data.HitNomalizedTime);

        if (target)
            ApplyHit(target, this, RemoteConfigManager.Instance.GameData.Value.AttackHitData.Dictionary[data.AttackHitId], dir);

        yield return new WaitForSeconds(data.MotionTime * (1 - data.HitNomalizedTime));

        _attackCooltime = data.Cooltime;
        _attackCoroutine = null;

        _unitAnimator.SetFloat("AnimationSpeed", 1f);
        _unitAnimator.Play(_unitData.CodeName, 0, 0f);
    }
    private IEnumerator Attack_Projectile(FieldObject target, Attack_ProjectileData data)
    {
        var clip = _unitAnimator.runtimeAnimatorController.animationClips.First(c => c.name == data.MotionAnimation);

        _unitAnimator.Play(data.MotionAnimation, 0, 0f);

        if (target)
            SummonProjectile(target, RemoteConfigManager.Instance.GameData.Value.ProjectileData.Dictionary[data.ProjectileId]);

        yield return new WaitForSeconds(clip.length);

        _attackCooltime = data.Cooltime;
        _attackCoroutine = null;
        _unitAnimator.Play(_unitData.CodeName, 0, 0f);
    }
    private void SummonProjectile(FieldObject target, ProjectileData data)
    {
        Vector3 position = data.SummonPoint switch
        {
            ProjectileSummonPoint.UnitCenter => BodyColliderCenter,
            ProjectileSummonPoint.UnitGround => transform.position,
            _ => BodyColliderCenter,
        };

        GameObject go = Instantiate(_projectilePrefab, position, Quaternion.identity);
        Projectile projectile = go.GetComponent<Projectile>();
        projectile.Init(this, target, data);
        projectile.NetworkObject.SpawnWithOwnership(OwnerClientId);
    }
}
