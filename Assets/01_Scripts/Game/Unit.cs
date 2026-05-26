using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class Unit : FieldObject
{
    public override Collider2D Collider => _collider;

    public NetworkVariable<int> CardId { get; set; } = new();

    [SerializeField, ComponentField] private SpriteRenderer _spriteRenderer;
    [SerializeField, ChildField] private Collider2D _colliderNormal;
    [SerializeField, ChildField] private Collider2D _colliderSmall;

    private int _cardId;
    private CardData _cardData;
    private Player _owner;
    private Player _opponent;
    private Collider2D _collider;

    public void Init(int cardId, Player owner, Player opponent)
    {
        _owner = owner;
        _opponent = opponent;

        _cardId = cardId;
        _cardData = StaticDB.Instance.CardDataTable[_cardId];
        _collider = _cardData.ColliderType switch
        {
            ColliderType.Normal => _colliderNormal,
            ColliderType.Small => _colliderSmall,
            _ => _colliderNormal,
        };
        _collider.enabled = true;
    }
    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            CardId.Value = _cardId;
        }
        else
        {
            _cardId = CardId.Value;
            _cardData = StaticDB.Instance.CardDataTable[_cardId];
        }

        string path = $"UnitSprite/Unit_{_cardData.CodeName}";
        Sprite sprite = Resources.Load<Sprite>(path);

        _spriteRenderer.sprite = sprite;
    }

    private void FixedUpdate()
    {
        UpdateTarget();
    }
    
    public float FindNearestTarget(out FieldObject find)
    {
        if (_cardData.TargetingType == TargetingType.Core)
        {
            find = _opponent.Core;
            return ColliderDistanceTo(find);
        }

        find = null;
        float distance = float.PositiveInfinity;
        
        HashSet<FieldObject> objects = _cardData.TargetingType switch
        {
            TargetingType.Ground => _opponent.GroundObjects,
            TargetingType.GroundOrAir => _opponent.AllObjects,
            _ => null
        };

        foreach (FieldObject obj in objects)
        {
            var dist = ColliderDistanceTo(obj);
            if (dist < distance)
            {
                distance = dist;
                find = obj;
            }
        }
        return distance;
    }
    public void UpdateTarget()
    {
        /*
        만약 (공격 대상 != 널 && 공격대상 in 공격범위) 
	        공격
        아니면 만약 (공격범위에 적 존재)
	        가까운 적 공격대상 설정
	        공격
        아니면 만약 (추적범위에 적 존재)
	        가까운적에게 이동
        아니면 
	        코어로 이동 
        */

        //var attackRange = Unit.AttackRange;
        //var chaseRange = Unit.ChaseRange;

        //if (Unit.AttackTarget != null && DistanceTo(Unit.AttackTarget) <= attackRange)
        //    return;

        //var distance = FindTargetObject(out var unit);
        //if (distance <= attackRange)
        //{
        //    // 새 타겟 공격
        //    Unit.AttackTarget = unit;
        //}
        //else if (distance <= chaseRange)
        //{
        //    // 가까운적에게 이동
        //    Unit.ChaseTarget = unit;
        //}
        //else
        //{
        //    var coreDistance = FindTargetCore(out var core);
        //    if (coreDistance <= attackRange)
        //    {
        //        // 코어 공격
        //        Unit.AttackTarget = core;
        //    }
        //    else
        //    {
        //        // 코어로 이동
        //        Unit.ChaseTarget = core;
        //    }
        //}
    }
}
