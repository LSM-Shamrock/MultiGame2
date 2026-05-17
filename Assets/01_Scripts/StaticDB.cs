using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExcelAsset]
public class StaticDB : ScriptableObject
{
    private static StaticDB s_instance;
    public static StaticDB Instance =>s_instance ?? (s_instance = Resources.Load<StaticDB>("StaticDB"));


    [SerializeField] private List<CardData> _cards;
    private Dictionary<string, CardData> _cardDict;
    public IReadOnlyList<CardData> CardDataList => _cards;
    public IReadOnlyDictionary<string, CardData> CardDataTable => Instance._cardDict ?? (_cardDict = _cards.ToDictionary(e => e.CodeName));
}