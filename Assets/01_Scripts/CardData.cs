using System;

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


[Serializable]
public class CardData
{
    public int CardId;
    public string CodeName;
    public string DisplayName;
    public int CostMP;
    public float SummonY;
    public int Health;
    public LayerType LayerType;
    public TargetingType TargetingType;
}
