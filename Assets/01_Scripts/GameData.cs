using Newtonsoft.Json;
using System;
using System.Collections.Generic;

#region Enum
public enum AltitudeType { Ground, Air, }
public enum TargetingType { Core, Ground, GroundOrAir, }
public enum VerticalMoveType { None, Fall, UpDown, }
public enum AttackType { Motion, Projectile, }
public enum ProjectileSummonPoint { UnitCenter, UnitGround }
public enum ProjectileTargetPoint { TargetCenter, TargetGround }
public enum ProjectileFacingType { Rotate, FlipX }
public enum ProjectileCollisionTarget { Ground, GroundOrAir, }
#endregion

#region Data
[Serializable] public class CardData : TableData
{
    public override int Key => CardId;
    [JsonProperty] public int CardId { get; private set; }
    [JsonProperty] public string CodeName { get; private set; }
    [JsonProperty] public string DisplayName { get; private set; }
    [JsonProperty] public string Description { get; private set; }
    [JsonProperty] public int CostMP { get; private set; }
    [JsonProperty] public int UnitId { get; private set; }
}
[Serializable] public class UnitData : TableData
{
    public override int Key => UnitId;
    [JsonProperty] public int UnitId { get; private set; }
    [JsonProperty] public string CodeName { get; private set; }
    [JsonProperty] public string DisplayName { get; private set; }

    [JsonProperty] public float SummonHeight { get; private set; }
    [JsonProperty] public float Scale { get; private set; }
    [JsonProperty] public float ColliderWidth { get; private set; }
    [JsonProperty] public float ColliderHeight { get; private set; }

    [JsonProperty] public int Health { get; private set; }
    [JsonProperty] public AltitudeType AltitudeType { get; private set; }
    [JsonProperty] public TargetingType TargetingType { get; private set; }
    [JsonProperty] public bool IsKnockbackIgnore { get; private set; }

    [JsonProperty] public string MoveAnimation { get; private set; }
    [JsonProperty] public float MoveSpeed { get; private set; }
    [JsonProperty] public float BackoffRatio { get; private set; }
    [JsonProperty] public float BackoffSpeedRatio { get; private set; }
    [JsonProperty] public VerticalMoveType VerticalMoveType { get; private set; }
    [JsonProperty] public int VerticalMoveId { get; private set; }

    [JsonProperty] public float AttackRange { get; private set; }
    [JsonProperty] public AttackType AttackType { get; private set; }
    [JsonProperty] public int AttackId { get; private set; }
}
[Serializable] public class VerticalMove_FallData : TableData
{
    public override int Key => VerticalMoveId;
    [JsonProperty] public int VerticalMoveId { get; private set; }
    [JsonProperty] public float FallSpeed { get; private set; }
}
[Serializable] public class VerticalMove_UpDownData : TableData
{
    public override int Key => VerticalMoveId;
    [JsonProperty] public int VerticalMoveId { get; private set; }
    [JsonProperty] public float UpHeight { get; private set; }
    [JsonProperty] public float DownHeight { get; private set; }
    [JsonProperty] public float UpSpeed { get; private set; }
    [JsonProperty] public float DownSpeed { get; private set; }
}
[Serializable] public class AttackData : TableData
{
    public override int Key => AttackId;
    [JsonProperty] public int AttackId { get; private set; }
    [JsonProperty] public string CodeName { get; private set; }
    [JsonProperty] public string DisplayName { get; private set; }
    [JsonProperty] public float Cooltime { get; private set; }
}
[Serializable] public class Attack_MotionData : AttackData
{
    [JsonProperty] public float MotionTime { get; private set; }
    [JsonProperty] public string MotionAnimation { get; private set; }
    [JsonProperty] public float HitNomalizedTime { get; private set; }
    [JsonProperty] public int AttackHitId { get; private set; }
}
[Serializable] public class Attack_ProjectileData : AttackData
{
    [JsonProperty] public string MotionAnimation { get; private set; }
    [JsonProperty] public int ProjectileId { get; private set; }
}
[Serializable] public class ProjectileData : TableData
{
    public override int Key => ProjectileId;
    [JsonProperty] public int ProjectileId { get; private set; }
    [JsonProperty] public string CodeName { get; private set; }
    [JsonProperty] public string DisplayName { get; private set; }

    [JsonProperty] public float Scale { get; private set; }
    [JsonProperty] public float ColliderWidth { get; private set; }
    [JsonProperty] public float ColliderHeight { get; private set; }
    [JsonProperty] public float ColliderOffsetX { get; private set; }
    [JsonProperty] public float ColliderOffsetY { get; private set; }
    [JsonProperty] public string SortingLayerName { get; private set; }
    [JsonProperty] public ProjectileSummonPoint SummonPoint { get; private set; }
    [JsonProperty] public ProjectileTargetPoint TargetPoint { get; private set; }
    [JsonProperty] public ProjectileFacingType FacingType { get; private set; }

    [JsonProperty] public float Speed { get; private set; }
    [JsonProperty] public float MaxDistance { get; private set; }
    [JsonProperty] public ProjectileCollisionTarget CollisionTarget { get; private set; }
    [JsonProperty] public bool IsPierce { get; private set; }
    [JsonProperty] public float PierceHitInterval { get; private set; }
    [JsonProperty] public int AttackHitId { get; private set; }
}
[Serializable] public class AttackHitData : TableData
{
    public override int Key => AttackHitId;
    [JsonProperty] public int AttackHitId { get; private set; }
    [JsonProperty] public string CodeName { get; private set; }
    [JsonProperty] public int Damage { get; private set; }
    [JsonProperty] public float KnockbackDistance { get; private set; }
    [JsonProperty] public float KnockbackSpeed { get; private set; }
    [JsonProperty] public float DrainRatio { get; private set; }
    [JsonProperty] public string EffectAnimation { get; private set; }
    [JsonProperty] public float EffectTime { get; private set; }
    [JsonProperty] public int DotEffectId { get; private set; }
}
[Serializable] public class DotEffectData : TableData
{
    public override int Key => DotEffectId;
    [JsonProperty] public int DotEffectId { get; private set; }
    [JsonProperty] public string CodeName { get; private set; }
    [JsonProperty] public string DisplayName { get; private set; }
    [JsonProperty] public int DotDamage { get; private set; }
    [JsonProperty] public float DotInterval { get; private set; }
    [JsonProperty] public float DotCount { get; private set; }
    [JsonProperty] public string EffectAnimation { get; private set; }
}
#endregion

[Serializable]
public class GameData
{
    [JsonProperty] private List<CardData> Card;
    [JsonProperty] private List<UnitData> Unit;
    [JsonProperty] private List<VerticalMove_FallData> VerticalMove_Fall;
    [JsonProperty] private List<VerticalMove_UpDownData> VerticalMove_UpDown;
    [JsonProperty] private List<Attack_MotionData> Attack_Motion;
    [JsonProperty] private List<Attack_ProjectileData> Attack_Projectile;
    [JsonProperty] private List<ProjectileData> Projectile;
    [JsonProperty] private List<AttackHitData> AttackHit;
    [JsonProperty] private List<DotEffectData> DotEffect;

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