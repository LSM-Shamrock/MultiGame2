using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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