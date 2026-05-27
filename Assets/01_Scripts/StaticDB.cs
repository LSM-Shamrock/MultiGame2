using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AltitudeType
{
    Ground,
    Air,
}
public enum TargetingType
{
    Core,
    Ground,
    GroundOrAir,
}
public enum ColliderType
{
    Normal,
    Small,
}
public enum MoveType
{
    Horizontal,
    Directional,
}
public enum AttackRangeType
{
    Horizontal,
    Directional,
}
public enum AttackType
{
    Motion,
    Projectile,
    Wave,
}

[Serializable]
public class CardData
{
    public int CardId;
    public string CodeName;
    public string DisplayName;
    public int CostMP;
    public int UnitId;
}

[Serializable]
public class UnitData
{
    public int UnitId;
    public string CodeName;
    public string DisplayName;
    public float SummonHeight;
    public int Health;
    public AltitudeType AltitudeType;
    public TargetingType TargetingType;
    public ColliderType ColliderType;
    public MoveType MoveType;
    public AttackRangeType AttackRangeType;
    public float AttackRange;
    public AttackType AttackType;
    public int AttackHitId;
}

[Serializable]
public class AttackHitData
{
    public int AttackHitId;
    public string CodeName;
    public int Damage;
    public float Knockback;
}

[ExcelAsset]
public class StaticDB : ScriptableObject
{
    private static StaticDB s_instance;
    public static StaticDB Instance => s_instance ?? (s_instance = Resources.Load<StaticDB>(nameof(StaticDB)));

    [SerializeField] private List<CardData> _Card;
    [SerializeField] private List<UnitData> _Unit;
    [SerializeField] private List<AttackHitData> _AttackHit;

    private Dictionary<int, CardData> _CardDictionary;
    private Dictionary<int, UnitData> _UnitDictionary;
    private Dictionary<int, AttackHitData> _AttackHitDictionary;

    public IReadOnlyList<CardData> CardDataList => _Card;    
    public IReadOnlyList<UnitData> UnitDataList => _Unit;
    public IReadOnlyList<AttackHitData> AttackHitDataList => _AttackHit;

    public IReadOnlyDictionary<int, CardData> CardDataTable => _CardDictionary ?? (_CardDictionary = _Card.ToDictionary(e => e.CardId));
    public IReadOnlyDictionary<int, UnitData> UnitDataTable => _UnitDictionary ?? (_UnitDictionary = _Unit.ToDictionary(e => e.UnitId));
    public IReadOnlyDictionary<int, AttackHitData> AttackHitDataTable => _AttackHitDictionary ?? (_AttackHitDictionary = _AttackHit.ToDictionary(e => e.AttackHitId));
}