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


[ExcelAsset]
public class StaticDB : ScriptableObject
{
    private static StaticDB s_instance;
    public static StaticDB Instance => s_instance ?? (s_instance = Resources.Load<StaticDB>(nameof(StaticDB)));


    [SerializeField] private List<CardData> _cards;
    [SerializeField] private List<UnitData> _units;


    private Dictionary<int, CardData> _cardDictionary;
    private Dictionary<int, UnitData> _unitDictionary;
    public IReadOnlyList<CardData> CardDataList => _cards;    
    public IReadOnlyList<UnitData> UnitDataList => _units;

    public IReadOnlyDictionary<int, CardData> CardDataTable
    {
        get
        {
            if (_cardDictionary == null)
            {
                _cardDictionary = _cards.ToDictionary(e => e.CardId);
            }
            return _cardDictionary;
        }
    }
    public IReadOnlyDictionary<int, UnitData> UnitDataTable
    {
        get
        {
            if (_unitDictionary == null)
            {
                _unitDictionary = _units.ToDictionary(e => e.UnitId);
            }
            return _unitDictionary;
        }
    }
}