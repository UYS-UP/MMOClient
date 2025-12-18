using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;


public enum SkillCastInputType
{
    None,           // 不需要额外选择（自施、直接朝前释放、锁定当前目标这种）
    UnitTarget,     // 选中一个单位（点怪、点队友）
    Direction,      // 选一个方向（通常以自己为原点：扇形、直线冲刺等）
    GroundPosition, // 在地面选一个点（AOE 落地圈）
}

public enum SkillAreaShape
{
    None,  
    Sector, 
    Circle,  
    Line, 
    Box,       
}

[Serializable]
public class SkillConfig
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public int Level { get; set; }
    public float Cooldown { get; set; }
    public float ManaCost { get; set; }
    public List<int> PreRequisiteSkills { get; set; }

    // 👇 多态字段：不同技能类型的配置放这里
    [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
    public SkillCastConfig Cast { get; set; }
}

[JsonObject(MemberSerialization.OptIn)]
public abstract class SkillCastConfig
{
    // 输入方式（点目标/点地/方向）
    [JsonProperty]
    public abstract SkillCastInputType InputType { get; }

    // 范围形状（扇形/圆形/直线）
    [JsonProperty]
    public SkillAreaShape AreaShape { get; protected set; } = SkillAreaShape.None;
    
    
    
}

public class NoneCastConfig : SkillCastConfig
{
    public override SkillCastInputType InputType => SkillCastInputType.None;
    public NoneCastConfig()
    {
        AreaShape = SkillAreaShape.None;
    }
}

public class MeleeSectorCastConfig : SkillCastConfig
{
    public override SkillCastInputType InputType => SkillCastInputType.Direction;

    // 扇形半径和角度
    [JsonProperty]
    public float Radius { get; set; } = 3f;

    [JsonProperty]
    public float Angle { get; set; } = 120f;

    public MeleeSectorCastConfig()
    {
        AreaShape = SkillAreaShape.Sector;
    }
}

public class GroundCircleCastConfig : SkillCastConfig
{
    public override SkillCastInputType InputType => SkillCastInputType.GroundPosition;

    // 落地点与角色的最大距离
    [JsonProperty]
    public float CastMaxDistance { get; set; } = 12f;

    // AOE 范围半径
    [JsonProperty]
    public float Radius { get; set; } = 5f;

    public GroundCircleCastConfig()
    {
        AreaShape = SkillAreaShape.Circle;
    }
}

public class DirectionLineCastConfig : SkillCastConfig
{
    public override SkillCastInputType InputType => SkillCastInputType.Direction;

    [JsonProperty]
    public float Length { get; set; } = 10f;

    [JsonProperty]
    public float Width { get; set; } = 1.5f;

    public DirectionLineCastConfig()
    {
        AreaShape = SkillAreaShape.Line;
    }
}

public class UnitTargetCastConfig : SkillCastConfig
{
    public override SkillCastInputType InputType => SkillCastInputType.UnitTarget;
    [JsonProperty]
    public float CastMaxDistance { get; set; } = 12f;

    public UnitTargetCastConfig()
    {
        AreaShape = SkillAreaShape.None;
    }
}