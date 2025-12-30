
using MessagePack;
using UnityEngine;

[MessagePackObject]
 public class ClientHeartPing
 {
     [Key(0)] public long ClientUtcMs;
 }

 [MessagePackObject]
 public class ClientPlayerLogin
 {
     [Key(0)] public string Username { get; set; }
     [Key(1)] public string Password { get; set; }
 }

 [MessagePackObject]
 public class ClientPlayerRegister
 {
     [Key(0)] public string Username { get; set; }
     [Key(1)] public string Password { get; set; }
     [Key(2)] public string RePassword { get; set; }
     [Key(3)] public string Code { get; set; }
 }

 [MessagePackObject]
 public class ClientCreateCharacter
 {
     [Key(0)] public string CharacterName { get; set; }
     [Key(1)] public int ServerId { get; set; }
 }

 [MessagePackObject]
 public class ClientCharacterMove
 {
     [Key(0)] public int ClientTick;
     [Key(1)] public short[] Position;
     [Key(2)] public short Yaw;
     [Key(3)] public sbyte[] Direction;
 }

 [MessagePackObject]
 public class ClientCharacterCastSkill
 {
     [Key(0)] public int ClientTick;
     [Key(1)] public int SkillId;

     [Key(2)] public SkillCastInputType InputType;
     [Key(3)] public Vector3 TargetPosition;
     [Key(4)] public Vector3 TargetDirection;
     [Key(5)] public string TargetEntityId;
 }


 [MessagePackObject]
 public class ClientQueryInventory
 {
     [Key(0)] public int StartSlot;
     [Key(1)] public int EndSlot;
 }

 [MessagePackObject]
 public class ClientSwapStorageSlot
 {
     [Key(0)] public int ReqId;
     [Key(1)] public SlotKey Slot1;
     [Key(2)] public SlotKey Slot2;
 }

 [MessagePackObject]
 public class ClientQuestAccept
 {
     [Key(0)] public string QuestId;
 }

 [MessagePackObject]
 public class ClientQuestSubmit
 {
     [Key(0)] public string QuestId;
     [Key(1)] public string TargetId;
 }

 [MessagePackObject]
 public class ClientPickItem
 {
     [Key(0)] public string ItemId;
     [Key(1)] public string EntityId;
 }

 [MessagePackObject]
 public class ClientCreateDungeonTeam
 {
     [Key(0)] public string TemplateId;
     [Key(1)] public string TeamName;
 }

 [MessagePackObject]
 public class ClientEnterGame { 
     [Key(0)] public string CharacterId; 
 }

 [MessagePackObject]
 public class ClientEnterRegion
 {
     [Key(0)] public int RegionId;
 }

 [MessagePackObject]
 public class ClientExitGame { }

 [MessagePackObject]
 public class ClientEnterDungeon { }

 [MessagePackObject]
 public class ClientExitDungeon { }

[MessagePackObject]
 public class ClientAddFriend
 {
     [Key(0)] public string CharacterName;
 }

 [MessagePackObject]
 public class ClientHandleAddFriendRequest
 {
     [Key(0)] public string RequestId;
     [Key(1)] public bool IsAccept;
 }

 [MessagePackObject]
 public class ClientDeleteFriend
 {
     
 }

 [MessagePackObject]
 public class ClientDungeonLootChoice
 {
     [Key(0)] public bool IsRoll;
     [Key(1)] public string ItemId;
 }