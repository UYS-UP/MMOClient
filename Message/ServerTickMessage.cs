using MessagePack;
using System.Collections.Generic;
using UnityEngine;


[MessagePackObject]
public class TickMessage
{
    [Key(0)] public int Tick;
}


[MessagePackObject]
public class ServerEntityCastSkill : TickMessage
{
    [Key(1)] public int Caster;
    [Key(2)] public int SkillId;
    [Key(3)] public short[] Position;
    [Key(4)] public short Yaw;
    [Key(5)] public EntityState State;
}

[MessagePackObject]
public class ServerPlayerReleaseSkill : TickMessage
{
    //[Key(1)] public int ClientTick;
    //[Key(2)] public int SkillId;
    //// [Key(3)] public EntityStateType State;
    //[Key(4)] public bool Success;
    //[Key(5)] public string Message;

    //public ServerPlayerReleaseSkill()
    //{
    //}

    //public ServerPlayerReleaseSkill(int tick, int clientTick, int skillId, EntityStateType state, bool success, string message)
    //{
    //    Tick = tick;
    //    ClientTick = clientTick;
    //    SkillId = skillId;
    //    State = state;
    //    Success = success;
    //    Message = message;
    //}
}


[MessagePackObject]
public class ServerEntityMoveSync : TickMessage
{
    [Key(1)] public int EntityId;
    [Key(2)] public EntityType Type;
    [Key(3)] public short[] Position;
    [Key(4)] public short Yaw;
    [Key(5)] public sbyte[] Direction;
    [Key(6)] public EntityState State;
    [Key(8)] public float Speed;
}


[MessagePackObject]
public class ServerPlayerMoveSync : TickMessage
{
    [Key(1)] public int ClientTick;
    [Key(2)] public int ServerTick;
    [Key(3)] public int EntityId;
    [Key(4)] public short[] Position;
    [Key(5)] public short Yaw;
    [Key(6)] public sbyte[] Direction;
    [Key(7)] public float Speed;
    [Key(8)] public bool IsValid;
}


[MessagePackObject]
public class ServerEntitySpawn : TickMessage
{
    [Key(1)] public NetworkEntity SpawnEntity;
}


[MessagePackObject]
public class ServerEntityDespawn : TickMessage
{
    [Key(1)] public HashSet<int> DespawnEntities;
}




[MessagePackObject]
public class ServerEntityDamage : TickMessage
{
    [Key(1)] public int Source;
    [Key(2)] public List<EntityWound> Wounds;
    [Key(3)] public List<EntityDeath> Deaths;
}

[MessagePackObject]
public class EntityWound
{
    [Key(0)] public float Wound;
    [Key(1)] public int Target;
    [Key(2)] public float CurrentHp;
}

[MessagePackObject]
public class EntityDeath
{
    [Key(0)] public float Wound;
    [Key(1)] public int Target;
    [Key(2)] public List<ItemData> DroppedItems;
}


[MessagePackObject]
public class ServerMonsterDeath : TickMessage
{
    [Key(1)] public string EntityId;
}

