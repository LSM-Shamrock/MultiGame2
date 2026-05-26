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
    public float ChaseRange;
    public float AttackRange;
}
