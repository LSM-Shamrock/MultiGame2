using System;
using System.Collections;
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

        Debug.Log("!");
        StartCoroutine(Routine());
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


    public void FindNearestTarget(out FieldObject find, out float distance)
    {
        find = _opponent.Core;
        distance = GetDistance(find);

        if (_cardData.TargetingType == TargetingType.Core)
            return;

        HashSet<Unit> units = _cardData.TargetingType switch
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
    public void FindNearestHorizontalTarget(out FieldObject find, out float distance)
    {
        find = _opponent.Core;
        distance = GetHorizontalDistance(find);

        if (_cardData.TargetingType == TargetingType.Core)
            return;

        HashSet<Unit> units = _cardData.TargetingType switch
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
            yield return new WaitForFixedUpdate();

            FindNearestHorizontalTarget(out var target, out float distance);


            float xDir = target.transform.position.x - transform.position.x;
            xDir = xDir == 0 ? 0 : xDir / Mathf.Abs(xDir);

            transform.position += Vector3.right * xDir * Time.fixedDeltaTime * 1f;
        }
    }
}
