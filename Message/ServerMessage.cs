using System.Collections.Generic;
using MessagePack;
using UnityEngine;

[MessagePackObject]
public class TickMessage
{
    [Key(0)] public int Tick;
}

[MessagePackObject]
public class ServerHeartPong
{
    [Key(0)] public long ServerUtcMs;
    [Key(1)] public long EchoClientUtcMs;
    [Key(2)] public int Tick;
}


[MessagePackObject]
public class ServerEntityReleaseSkill : TickMessage
{
    [Key(1)] public string ReleaserId;
    [Key(2)] public int SkillId;
    [Key(3)] public short[] Position;
    [Key(4)] public short Yaw;
    [Key(5)] public EntityStateType State;
}

[MessagePackObject]
public class ServerPlayerReleaseSkill : TickMessage
{
    [Key(1)] public int ClientTick;
    [Key(2)] public int SkillId;
    [Key(3)] public EntityStateType State;
    [Key(4)] public bool Success;
    [Key(5)] public string Message;
}

[MessagePackObject]
public class ServerSkillTimelineEventMove : TickMessage
{
    [Key(1)] public string EntityId;
    [Key(2)] public int SkillId;
    [Key(3)] public Vector3 EndPos;
    [Key(4)] public float Time;
}

[MessagePackObject]
public class ServerApplyBuff
{
    [Key(0)] public int BuffId;
    [Key(1)] public float Duration;
}


[MessagePackObject]
public class ServerEntityMoveSync : TickMessage
{
    [Key(1)] public string EntityId;
    [Key(2)] public EntityType Type;
    [Key(3)] public short[] Position;
    [Key(4)] public short Yaw;
    [Key(5)] public sbyte[] Direction;
    [Key(6)] public MotionStateType Motion;
    [Key(7)] public ActionStateType Action;
    [Key(8)] public float Speed;
}

[MessagePackObject]
public class ServerPlayerMoveSync : TickMessage
{
    [Key(1)] public int ClientTick;
    [Key(2)] public int ServerTick;
    [Key(3)] public string EntityId;
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
    [Key(1)] public HashSet<string> DespawnEntities;
}




[MessagePackObject]
public class ServerEntityDamage : TickMessage
{
    [Key(1)] public string Source;
    [Key(2)] public List<EntityWound> Wounds;
    [Key(3)] public List<EntityDeath> Deaths;
}

[MessagePackObject]
public class EntityWound
{
    [Key(0)] public float Wound;
    [Key(1)] public string Target;
    [Key(2)] public int CurrentHp;
}

[MessagePackObject]
public class EntityDeath
{
    [Key(0)] public float Wound;
    [Key(1)] public string Target;
    [Key(2)] public List<ItemData> DroppedItems;
}


[MessagePackObject]
public class ServerQueryInventory
{
    [Key(0)] public int MaxSize;
    [Key(1)] public Dictionary<SlotKey, ItemData> Data;
    [Key(2)] public int MaxOccupiedSlot;
}

[MessagePackObject]
public class ServerSwapStorageSlotResponse
{
    [Key(0)] public int ReqId;
    [Key(1)] public bool Success;
    [Key(2)] public ItemData Item1;
    [Key(3)] public ItemData Item2;
}

[MessagePackObject]
public class ServerAddItem
{
    [Key(0)] public Dictionary<SlotKey, ItemData> Items;
    [Key(1)] public int MaxSize;

}


[MessagePackObject]
public class ServerQuestListSync
{
    [Key(0)] public List<QuestNode> Quests;
}

[MessagePackObject]
public class ServerMonsterDeath : TickMessage
{
    [Key(1)] public string EntityId;
}

[MessagePackObject]
public class ServerPlayerPickupItem
{
    [Key(0)] public ItemData Data;
    [Key(1)] public int InventorySlot;
}


[MessagePackObject]
public class ServerCreateDungeonTeam
{
    [Key(0)] public bool Success;
    [Key(1)] public string Message;
    [Key(2)] public TeamBaseData Team;
}

[MessagePackObject]
public class ServerDungeonTeamInvite
{
    [Key(0)] public int TeamId;
    [Key(1)] public string Message;
}



[MessagePackObject]
public class ServerPlayerEnterGame
{
    [Key(0)] public NetworkEntity PlayerEntity;
}

[MessagePackObject]
public class ServerLoadDungeon
{
    [Key(0)] public string TemplateId;
}


[MessagePackObject]
public class ServerLevelRegion
{
    [Key(0)] public string RegionId;
}

[MessagePackObject]
public class ServerLevelDungeon
{
    [Key(0)] public string Cause;
    [Key(1)] public string RegionId;
}

[MessagePackObject]
public class ServerPlayerEnterDungeon
{
    [Key(0)] public NetworkEntity PlayerEntity;
    [Key(1)] public float LimitTime;
}

[MessagePackObject]
public class ServerDungeonLootChoice
{
    [Key(0)] public string EntityName;
    [Key(1)] public string ItemId;
    [Key(2)] public LootChoiceType LootChoiceType;
    [Key(3)] public int RollValue;
}

[MessagePackObject]
public class ServerPlayerEnterTeam
{
    [Key(0)] public bool Success;
    [Key(1)] public string Message;
    [Key(2)] public TeamBaseData Team;
    [Key(3)] public string Player;
}


[MessagePackObject]
public class ServerAddFriend
{
    [Key(0)] public bool Success;
    [Key(1)] public string Message;
}

[MessagePackObject]
public class ServerFriendListSync
{
    [Key(0)] public List<NetworkFriendGroupData> Groups;
    [Key(1)] public List<NetworkFriendRequestData> Requests;
    [Key(2)] public List<NetworkFriendData> Friends;
}

[MessagePackObject]
public class ServerAddFriendGroup
{
    [Key(0)] public bool Success;
    
}


