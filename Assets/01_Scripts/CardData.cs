using System;

[Flags]
public enum UnitLayer
{
    Ground = 1 << 0,
    Air = 1 << 1,  
}

[Serializable]
public class CardData
{
    public int CardId;
    public string CodeName;
    public string DisplayName;
    public int CostMP;
    public UnitLayer Layer;
    public float SummonY;
    public int Health;
}
