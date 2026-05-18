using System;
using UnityEngine;

[Serializable]
public class CardData
{
    public string CodeName;
    public string DisplayName;
    public int CostMP;

    public string SummonUnit => CodeName;
}
