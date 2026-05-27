using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum LayerType
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
    public float SummonHeight;
    public int Health;
    public LayerType LayerType;
    public TargetingType TargetingType;
    public ColliderType ColliderType;
}

[ExcelAsset]
public class StaticDB : ScriptableObject
{
    private static StaticDB s_instance;
    public static StaticDB Instance => s_instance ?? (s_instance = Resources.Load<StaticDB>(nameof(StaticDB)));


    [SerializeField] 
    private List<CardData> _cards;
    private Dictionary<int, CardData> _cardDictionary;

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
    public IReadOnlyList<CardData> CardDataList => _cards;    

}