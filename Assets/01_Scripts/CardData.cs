using System;

public class CardData
{
    public readonly string CodeName;
    public readonly string DisplayName;
    public readonly int CostMP;

    public string SummonUnit => CodeName;
}
