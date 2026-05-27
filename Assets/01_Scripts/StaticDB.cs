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
}

[Serializable]
public class HitData
{
    public int HitId;
    public string CodeName;
    public int Damage;
    public float Knockback;
}

[ExcelAsset]
public class StaticDB : ScriptableObject
{
    private static StaticDB s_instance;
    public static StaticDB Instance => s_instance ?? (s_instance = Resources.Load<StaticDB>(nameof(StaticDB)));

    [SerializeField] private List<CardData> _cardDatas;
    [SerializeField] private List<UnitData> _unitDatas;
    [SerializeField] private List<HitData> _hitDatas;

    private Dictionary<int, CardData> _cardDictionary;
    private Dictionary<int, UnitData> _unitDictionary;
    private Dictionary<int, HitData> _hitDictionary;

    public IReadOnlyList<CardData> CardDataList => _cardDatas;    
    public IReadOnlyList<UnitData> UnitDataList => _unitDatas;
    public IReadOnlyList<HitData> HitDataList => _hitDatas;

    public IReadOnlyDictionary<int, CardData> CardDataTable => _cardDictionary ?? (_cardDictionary = _cardDatas.ToDictionary(e => e.CardId));
    public IReadOnlyDictionary<int, UnitData> UnitDataTable => _unitDictionary ?? (_unitDictionary = _unitDatas.ToDictionary(e => e.UnitId));
    public IReadOnlyDictionary<int, HitData> HitDataTable => _hitDictionary ?? (_hitDictionary = _hitDatas.ToDictionary(e => e.HitId));
}