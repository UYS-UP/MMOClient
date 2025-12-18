using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Protocol : ushort
{
    Heart,
    Login,
    Register,
    CreateCharacter,

    
    EnterGame,
    EnterRegion,
    LevelRegion,
    
    CreateDungeonTeam,
    StartDungeon,
    LoadDungeon,
    EnterDungeon,
    LevelDungeon,
    DungeonLootChoice,
    DungeonLootInfo,

    EntitySpawn,
    EntityDespawn,
    EntityMove,
    PlayerMove,
    EntityReleaseSkill,
    PlayerReleaseSkill,
    ApplyBuff,
    
    MonsterDeath,
    EntityDamage,
    
    QueryInventory,
    AddInventoryItem,
    SwapStorageSlot,
    
    QuestListSync,
    QuestAccept,
    QuestCompleted,
    QuestUpdated,
    


    InvitePlayer,

    AcceptInvite,
    EnterTeam,
    ChatMessage,
    SkillTimelineEvent,
    
    AddFriend,
    AddFriendRequest,
    HandleFriendRequest,
    AddFriendGroup,
    FriendListSync,


}



public enum StateCode
{
    Success = 200,          // 成功
    BadRequest = 400,      // 错误请求
    Unauthorized = 401,    // 未授权
    NotFound = 404,        // 未找到
    InternalError = 500,   // 服务器内部错误
}
