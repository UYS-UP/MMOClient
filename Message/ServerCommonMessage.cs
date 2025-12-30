using System;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;


[MessagePackObject]
public class ServerPlayerRegister
{
    [Key(0)] public bool Sucess;
    [Key(1)] public string Message;
    [Key(2)] public string Username;
}

[MessagePackObject]
public class ServerPlayerLogin
{
    [Key(0)] public bool Sucess;
    [Key(1)] public string Message;
    [Key(2)] public NetworkPlayer Player;
    [Key(3)] public List<NetworkCharacterPreview> Previews;
}


[MessagePackObject]
public class NetworkCharacterPreview
{
    [Key(0)] public string CharacterId;
    [Key(1)] public string CharacterName;
    [Key(3)] public int Level;
    [Key(4)] public int MapId;
    [Key(5)] public int ServerId;
    [Key(6)] public DateTime LastLoginTime;
}

[MessagePackObject]
public class ServerCreateCharacter
{
    [Key(0)] public bool Success;
    [Key(1)] public string Message;
    [Key(2)] public string CharacterId;
}









[MessagePackObject]
public class ServerHeartPong
{
    [Key(0)] public long ServerUtcMs;
    [Key(1)] public long EchoClientUtcMs;
    [Key(2)] public int Tick;

    public ServerHeartPong(long serverUtcMs, int serverTick, long echoClientUtcMs)
    {
        ServerUtcMs = serverUtcMs;
        Tick = serverTick;
        EchoClientUtcMs = echoClientUtcMs;
    }

    public ServerHeartPong()
    {
    }
}




[MessagePackObject]
public class ServerLevelDungeon
{
    [Key(0)] public string Cause;
    [Key(1)] public int MapId;
}


[MessagePackObject]
public class ServerApplyBuff
{
    [Key(0)] public int BuffId;
    [Key(1)] public float Duration;
}




[MessagePackObject]
public class ServerQueryInventory
{
    [Key(0)] public int MaxSize;
    [Key(1)] public Dictionary<SlotKey, ItemData> Data;
    [Key(2)] public int MaxOccupiedSlot;

    public ServerQueryInventory() { }

    public ServerQueryInventory(int maxSize, Dictionary<SlotKey, ItemData> data, int maxOccupiedSlot)
    {
        MaxSize = maxSize;
        Data = data;
        MaxOccupiedSlot = maxOccupiedSlot;
    }
}

[MessagePackObject]
public class ServerSwapStorageSlotResponse
{
    [Key(0)] public int ReqId;
    [Key(1)] public bool Success;
    [Key(2)] public ItemData Item1;
    [Key(3)] public ItemData Item2;

    public ServerSwapStorageSlotResponse()
    {
    }

    public ServerSwapStorageSlotResponse(int reqId, bool success, ItemData item1, ItemData item2)
    {
        ReqId = reqId;
        Success = success;
        Item1 = item1;
        Item2 = item2;
    }
}

[MessagePackObject]
public class ServerAddItem
{
    [Key(0)] public Dictionary<SlotKey, ItemData> Items;
    [Key(1)] public int MaxSize;

}

[MessagePackObject]
public class ServerQuestProgressUpdate
{
    [Key(0)] public List<QuestProgressUpdate> QuestUpdates;
}

[MessagePackObject]
public class QuestProgressUpdate
{
    [Key(0)] public string NodeId;
    [Key(1)] public bool IsCompleted;
    [Key(2)] public List<(int, QuestObjective)> Objectives;
}

[MessagePackObject]
public class ServerDungeonFailed
{

}




[MessagePackObject]
public class ServerCreateDungeonTeam
{
    [Key(0)] public bool Success;
    [Key(1)] public string Message;
    [Key(2)] public TeamData Team;

    public ServerCreateDungeonTeam()
    {
    }

    public ServerCreateDungeonTeam(bool success, string message, TeamData team)
    {
        Success = success;
        Message = message;
        Team = team;
    }
}

[MessagePackObject]
public class ServerLoadDungeon
{
    [Key(0)] public string TemplateId;
}

[MessagePackObject]
public class ServerDungeonTeamInvite
{
    [Key(0)] public int TeamId;
    [Key(1)] public string Message;

    public ServerDungeonTeamInvite(int teamId, string message)
    {
        TeamId = teamId;
        Message = message;
    }

    public ServerDungeonTeamInvite()
    {
    }
}



[MessagePackObject]
public class ServerPlayerEnterGame
{
    [Key(0)] public NetworkEntity PlayerEntity;

    public ServerPlayerEnterGame()
    {
    }

    public ServerPlayerEnterGame(NetworkEntity playerEntity)
    {
        PlayerEntity = playerEntity;
    }
}

[MessagePackObject]
public class ServerPlayerChangeDungeon
{
    [Key(0)] public string RegionId;
    [Key(1)] public string DungeonTemplateId;

    public ServerPlayerChangeDungeon()
    {
    }

    public ServerPlayerChangeDungeon(string regionId, string dungeonTemplateId)
    {
        RegionId = regionId;
        DungeonTemplateId = dungeonTemplateId;
    }
}

[MessagePackObject]
public class ServerDungeonCompleted
{
  
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
public class ServerPlayerEnterDungeon
{
    [Key(0)] public NetworkEntity PlayerEntity;
    [Key(1)] public float LimitTime;

    public ServerPlayerEnterDungeon()
    {
    }

    public ServerPlayerEnterDungeon(NetworkEntity playerEntity, float limitTime)
    {
        PlayerEntity = playerEntity;
        LimitTime = limitTime;
    }
}

[MessagePackObject]
public class ServerEnterTeam
{
    [Key(0)] public bool Success;
    [Key(1)] public string Message;
    [Key(2)] public TeamData Team;

    public ServerEnterTeam(bool success, string message, TeamData team)
    {
        Success = success;
        Message = message;
        Team = team;
    }

    public ServerEnterTeam()
    {
    }
}

[MessagePackObject]
public class ServerSkillTimelineEventMove : TickMessage
{
    [Key(1)] public string EntityId;
    [Key(2)] public int SkillId;
    [Key(3)] public Vector3 EndPos;
    [Key(4)] public float Time;

    public ServerSkillTimelineEventMove()
    {
    }

    public ServerSkillTimelineEventMove(int tick, string entityId, int skillId, Vector3 endPos, float time)
    {
        Tick = tick;
        EntityId = entityId;
        SkillId = skillId;
        EndPos = endPos;
        Time = time;
    }
}


[MessagePackObject]
public class ServerQuestListSync
{
    [Key(0)] public List<QuestNode> Quests;

    public ServerQuestListSync()
    {

    }
    public ServerQuestListSync(List<QuestNode> quests)
    {
        Quests = quests;
    }
}

[MessagePackObject]
public class ServerAddFriend
{
    [Key(0)] public bool Success;
    [Key(1)] public string Message;

    public ServerAddFriend()
    {
    }

    public ServerAddFriend(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}

[MessagePackObject]
public class ServerFriendListSync
{
    [Key(0)] public List<NetworkFriendGroupData> Groups;
    [Key(1)] public List<NetworkFriendRequestData> Requests;
    [Key(2)] public List<NetworkFriendData> Friends;

    public ServerFriendListSync()
    {
    }

    public ServerFriendListSync(List<NetworkFriendGroupData> groups, List<NetworkFriendRequestData> requests, List<NetworkFriendData> friends)
    {
        Groups = groups;
        Requests = requests;
        Friends = friends;
    }
}

[MessagePackObject]
public class ServerEnterRegion
{
    [Key(0)] public int MapId;
}

[MessagePackObject]
public class ServerEnterDungeon
{
    [Key(0)] public int DungeonTemplateId;
    [Key(1)] public float LimitTime;
}



[MessagePackObject]
public class ServerAddFriendGroup
{
    [Key(0)] public bool Success;

}


