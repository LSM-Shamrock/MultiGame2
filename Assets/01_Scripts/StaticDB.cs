using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


#region Enum
public enum AltitudeType { Ground, Air, }
public enum TargetingType { Core, Ground, GroundOrAir, }
public enum MoveType { Horizontal, Directional, }
public enum VerticalMoveType { None, Fall, UpDown, }
public enum AttackRangeType { Horizontal, Directional, }
public enum AttackType { Motion, Projectile, }
public enum ProjectileSummonPositionType { UnitCenter, UnitGround }
public enum ProjectileMoveType { Directional, Horizontal }
#endregion

#region Data
[Serializable]
public class CardData : TableData
{
    public override int Key => CardId;
    public int CardId;
    public string CodeName;
    public string DisplayName;
    public string Description;
    public int CostMP;
    public int UnitId;
}
[Serializable]
public class UnitData : TableData
{
    public override int Key => UnitId;
    public int UnitId;
    public string CodeName;
    public string DisplayName;

    public float SummonHeight;
    public float Scale;
    public float ColliderWidth;
    public float ColliderHeight;

    public int Health;
    public AltitudeType AltitudeType;
    public TargetingType TargetingType;
    public bool IsKnockbackIgnore;

    public MoveType MoveType;
    public string MoveAnimation;
    public float MoveSpeed;
    public float BackoffRatio;
    public float BackoffSpeedRatio;
    public VerticalMoveType VerticalMoveType;
    public int VerticalMoveId;

    public AttackRangeType AttackRangeType;
    public float AttackRange;
    public AttackType AttackType;
    public int AttackId;
}
[Serializable]
public class VerticalMove_FallData : TableData
{
    public override int Key => VerticalMoveId;
    public int VerticalMoveId;
    public float FallSpeed;
}
[Serializable]
public class VerticalMove_UpDownData : TableData
{
    public override int Key => VerticalMoveId;
    public int VerticalMoveId;
    public float UpHeight;
    public float DownHeight;
    public float UpSpeed;
    public float DownSpeed;
}
[Serializable]
public class AttackData : TableData
{
    public override int Key => AttackId;
    public int AttackId;
    public string CodeName;
    public string DisplayName;
    public float Cooltime;
}
[Serializable]
public class Attack_MotionData : AttackData
{
    public float MotionTime;
    public string MotionAnimation;
    public float HitNomalizedTime;
    public int AttackHitId;
}
[Serializable]
public class Attack_ProjectileData : AttackData
{
    public string MotionAnimation;
    public int ProjectileId;
}
[Serializable]
public class ProjectileData : TableData
{
    public override int Key => ProjectileId;
    public int ProjectileId;
    public string CodeName;
    public float Scale;
    public float ColliderWidth;
    public float ColliderHeight;
    public float ColliderOffsetX;
    public float ColliderOffsetY;
    public string SortingLayerName;
    public ProjectileSummonPositionType SummonPositionType;
    public ProjectileMoveType MoveType;
    public float Speed;
    public float MaxDistance;
    public bool IsPierce;
    public float PierceHitInterval;
    public int AttackHitId;
}
[Serializable]
public class AttackHitData : TableData
{
    public override int Key => AttackHitId;
    public int AttackHitId;
    public string CodeName;
    public int Damage;
    public float KnockbackDistance;
    public float KnockbackSpeed;
    public float DrainRatio;
    public string EffectAnimation;
    public float EffectTime;
    public int DotEffectId;
}
[Serializable]
public class DotEffectData : TableData
{
    public override int Key => DotEffectId;
    public int DotEffectId;
    public string CodeName;
    public int DotDamage;
    public float DotInterval;
    public float DotCount;
    public string EffectAnimation;
}
#endregion

public abstract class TableData
{
    public abstract int Key { get; }
}
public class Table<T> where T : TableData
{
    public IReadOnlyList<T> List { get; }
    public IReadOnlyDictionary<int, T> Dictionary { get; }
    public Table(IReadOnlyList<T> datas)
    {
        List = datas;
        Dictionary = datas.ToDictionary(e => e.Key);
    }
}
[ExcelAsset]
public class StaticDB : ScriptableObject
{
    private static StaticDB s_instance;
    public static StaticDB Instance => s_instance ?? (s_instance = Resources.Load<StaticDB>(nameof(StaticDB)));

    [SerializeField] private List<CardData> Card;
    [SerializeField] private List<UnitData> Unit;
    [SerializeField] private List<VerticalMove_FallData> VerticalMove_Fall;
    [SerializeField] private List<VerticalMove_UpDownData> VerticalMove_UpDown;
    [SerializeField] private List<Attack_MotionData> Attack_Motion;
    [SerializeField] private List<Attack_ProjectileData> Attack_Projectile;
    [SerializeField] private List<ProjectileData> Projectile;
    [SerializeField] private List<AttackHitData> AttackHit;
    [SerializeField] private List<DotEffectData> DotEffect;

    private Dictionary<Type, object> _tables = new();

    public Table<CardData> CardData => GetOrCreateTable(Card);
    public Table<UnitData> UnitData => GetOrCreateTable(Unit);
    public Table<AttackHitData> AttackHitData => GetOrCreateTable(AttackHit);
    public Table<VerticalMove_FallData> VerticalMove_FallData => GetOrCreateTable(VerticalMove_Fall);
    public Table<VerticalMove_UpDownData> VerticalMove_UpDownData => GetOrCreateTable(VerticalMove_UpDown);
    public Table<Attack_MotionData> Attack_MotionData => GetOrCreateTable(Attack_Motion);
    public Table<Attack_ProjectileData> Attack_ProjectileData => GetOrCreateTable(Attack_Projectile);
    public Table<ProjectileData> ProjectileData => GetOrCreateTable(Projectile);
    public Table<DotEffectData> DotEffectData => GetOrCreateTable(DotEffect);


    private Table<T> GetOrCreateTable<T>(IReadOnlyList<T> datas) where T : TableData
    {
        if (_tables.TryGetValue(typeof(T), out var obj))
        {
            return (Table<T>)obj;
        }
        else
        {
            var table = new Table<T>(datas);

            _tables.Add(typeof(T), table);

            return table;
        }
    }
}