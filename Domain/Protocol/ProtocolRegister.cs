using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ProtocolRegister : SingletonMono<ProtocolRegister>
{
    private MinHeap<IWorldEvent> worldEvents;


    protected override void Awake()
    {
        base.Awake();
        worldEvents = new MinHeap<IWorldEvent>((a, b) => a.Tick.CompareTo(b.Tick));

        CommonProtocolRegister();
        TickProtocolRegister();
    }
    
    
    private void CommonProtocolRegister()
    {
        GameClient.Instance.RegisterHandler(Protocol.Login, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.Register, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.CreateCharacter, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.SwapStorageSlot , ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.QueryInventory, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.AddInventoryItem, ExecuteNetworkEvent);
        
        GameClient.Instance.RegisterHandler(Protocol.CreateDungeonTeam, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.LoadDungeon, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.EnterDungeon, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.EnterGame, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.EnterTeam, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.InvitePlayer, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.ChatMessage, ExecuteNetworkEvent);
        
        
        GameClient.Instance.RegisterHandler(Protocol.FriendListSync, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.AddFriend, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.AddFriendRequest, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.HandleFriendRequest, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.AddFriendGroup, ExecuteNetworkEvent);
    }
    
    private void TickProtocolRegister()
    {
        GameClient.Instance.RegisterHandler(Protocol.EntitySpawn, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.EntityDespawn, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.EntityMove, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.PlayerMove, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.MonsterDeath, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.EntityReleaseSkill, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.EntityDamage, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.PlayerReleaseSkill, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.SkillTimelineEvent, ExecuteNetworkEvent);
    }
    
    public void ProcessWorldEvents(double renderServerTickExact)  // 参数重命名，语义清晰
    {
        int maxEventsPerFrame = 200;
        int processed = 0;

        while (worldEvents.Count > 0 && processed < maxEventsPerFrame)
        {
            if (!worldEvents.Peek(out var evt)) break;
            
            if (evt.Tick > renderServerTickExact) break;
            // Debug.Log($"RenderTick: {renderServerTickExact:F1}, EventTick: {evt.Tick}, Delay: {evt.Tick - renderServerTickExact:F1}");
            worldEvents.Pop();
            ExecuteWorldEvent(evt);
            processed++;
        }

        if (processed >= maxEventsPerFrame)
        {
            Debug.LogWarning($"[ProtocolRegister] Processed {processed} events, backlog detected! Heap size: {worldEvents.Count}");
        }
    }
    
    private void ExecuteNetworkEvent(GamePacket packet)
    {

        Protocol protocol = (Protocol)packet.ProtocolId;
        switch (protocol)
        {
            case Protocol.Login:
                OnLoginResponseEvent?.Invoke(packet.DeSerializePayload<ResponseMessage<NetworkPlayer>>());
                break;
            case Protocol.Register:
                OnRegisterResponseEvent?.Invoke(packet.DeSerializePayload<ResponseMessage<string>>());
                break;
            case Protocol.CreateCharacter:
                OnCreateCharacterResponseEvent?.Invoke(packet.DeSerializePayload<ResponseMessage<List<NetworkCharacter>>>());
                break;
            case Protocol.EnterGame:
                OnEnterGameResponseEvent?.Invoke(packet.DeSerializePayload<ServerPlayerEnterGame>());
                break;
            case Protocol.SwapStorageSlot:
                OnSwapInventorySlotResponseEvent?.Invoke(packet.DeSerializePayload<ServerSwapStorageSlotResponse>());
                break;
            case Protocol.QueryInventory:
                OnQueryInventoryEvent?.Invoke(packet.DeSerializePayload<ServerQueryInventory>());
                break;
            case Protocol.CreateDungeonTeam:
                OnCreateDungeonTeamEvent?.Invoke(packet.DeSerializePayload<ServerCreateDungeonTeam>());
                break;
            case Protocol.LoadDungeon:
                OnLoadDungeonEvent?.Invoke(packet.DeSerializePayload<ServerLoadDungeon>());
                break;
            case Protocol.LevelRegion:
                OnLevelRegionEvent?.Invoke(packet.DeSerializePayload<ServerLevelRegion>());
                break;
            case Protocol.LevelDungeon:
                OnLevelDungeonEvent?.Invoke(packet.DeSerializePayload<ServerLevelDungeon>());
                break;
            case Protocol.EnterDungeon:
                OnPlayerEnterDungeonEvent?.Invoke(packet.DeSerializePayload<ServerPlayerEnterDungeon>());
                break;
            case Protocol.InvitePlayer:
                OnTeamInvitePlayerEvent?.Invoke(packet.DeSerializePayload<ServerDungeonTeamInvite>());
                break;
            case Protocol.EnterTeam:
                OnPlayerEnterTeamEvent?.Invoke(packet.DeSerializePayload<ServerPlayerEnterTeam>());
                break;
            case Protocol.ChatMessage:
                OnChatMessageEvent?.Invoke(packet.DeSerializePayload<ChatMessageData>());
                break;
            case Protocol.AddFriend:
                OnAddFriendEvent?.Invoke(packet.DeSerializePayload<ServerAddFriend>());
                break;
            case Protocol.AddFriendRequest:
                OnAddFriendRequestEvent?.Invoke(packet.DeSerializePayload<NetworkFriendRequestData>());;
                break;
            case Protocol.HandleFriendRequest:
                OnHandleFriendRequestEvent?.Invoke(packet.DeSerializePayload<NetworkFriendData>());
                break;
            case Protocol.AddFriendGroup:
                OnAddFriendGroupEvent?.Invoke(packet.DeSerializePayload<NetworkFriendGroupData>());
                break;
            case Protocol.FriendListSync:
                OnFriendListSyncEvent?.Invoke(packet.DeSerializePayload<ServerFriendListSync>());
                break;
            case Protocol.AddInventoryItem:
                OnAddItemEvent?.Invoke(packet.DeSerializePayload<ServerAddItem>());
                break;

            
            case Protocol.EntitySpawn:
                var entitySpawn = packet.DeSerializePayload<ServerEntitySpawn>();
                worldEvents.Push(new WorldEvent<ServerEntitySpawn> { Tick = entitySpawn.Tick, Type = WorldEventType.EntitySpawn, Data = entitySpawn });
                break;
            case Protocol.EntityDespawn:
                var entityDespawn = packet.DeSerializePayload<ServerEntityDespawn>();
                worldEvents.Push(new WorldEvent<ServerEntityDespawn> { Tick = entityDespawn.Tick, Type = WorldEventType.EntityDespawn, Data = entityDespawn });
                break;
            case Protocol.EntityMove:
                var entityMove = packet.DeSerializePayload<ServerEntityMoveSync>();
                worldEvents.Push(new WorldEvent<ServerEntityMoveSync> { Tick = entityMove.Tick, Type = WorldEventType.EntityMove, Data = entityMove });
                break;
            case Protocol.PlayerReleaseSkill:
                var playerReleaseSkill = packet.DeSerializePayload<ServerPlayerReleaseSkill>();
                worldEvents.Push(new WorldEvent<ServerPlayerReleaseSkill> { Tick = playerReleaseSkill.Tick, Type = WorldEventType.PlayerReleaseSkill, Data = playerReleaseSkill });
                break;
            case Protocol.EntityReleaseSkill:
                var entityReleaseSkill = packet.DeSerializePayload<ServerEntityReleaseSkill>();
                worldEvents.Push(new WorldEvent<ServerEntityReleaseSkill> { Tick = entityReleaseSkill.Tick, Type = WorldEventType.EntityReleaseSkill, Data = entityReleaseSkill });
                break;
            case Protocol.PlayerMove:
                var playerMove = packet.DeSerializePayload<ServerPlayerMoveSync>();
                worldEvents.Push(new WorldEvent<ServerPlayerMoveSync> { Tick = playerMove.Tick, Type = WorldEventType.PlayerMove, Data = playerMove });
                break;
            case Protocol.EntityDamage:
                var entityDamage = packet.DeSerializePayload<ServerEntityDamage>();
                worldEvents.Push(new WorldEvent<ServerEntityDamage> { Tick = entityDamage.Tick, Type = WorldEventType.EntityHit, Data = entityDamage });
                break;
            case Protocol.MonsterDeath:
                var monsterDeath = packet.DeSerializePayload<ServerMonsterDeath>();
                worldEvents.Push(new WorldEvent<ServerMonsterDeath>() { Tick = monsterDeath.Tick, Type = WorldEventType.MonsterDeath, Data = monsterDeath });
                break;
            case Protocol.SkillTimelineEvent:
                var skillTimelineEvent = packet.DeSerializePayload<ServerSkillTimelineEventMove>();
                worldEvents.Push(new WorldEvent<ServerSkillTimelineEventMove>(){Tick = skillTimelineEvent.Tick, Type = WorldEventType.SkillTimelineEvent, Data = skillTimelineEvent });
                break;
        }
    }
    
    public string ToHex(ReadOnlyMemory<byte> mem)
    {
        var span = mem.Span;
        StringBuilder sb = new StringBuilder(span.Length * 3);
        for (int i = 0; i < span.Length; i++)
        {
            sb.AppendFormat("{0:X2} ", span[i]);
        }
        return sb.ToString();
    }
    
    private void ExecuteWorldEvent(IWorldEvent evt)
    {
        switch (evt.Type)
        {
            case WorldEventType.EntitySpawn: OnExecuteEntitySpawnEvent?.Invoke(((WorldEvent<ServerEntitySpawn>)evt).Data); break;
            case WorldEventType.EntityDespawn: OnExecuteEntityDespawnEvent?.Invoke(((WorldEvent<ServerEntityDespawn>)evt).Data); break;
            case WorldEventType.SkillTimelineEvent: OnExecuteSkillTimelineEvent?.Invoke(((WorldEvent<ServerSkillTimelineEventMove>)evt).Data); break;
            case WorldEventType.EntityHit: OnExecuteEntityDamageEvent?.Invoke(((WorldEvent<ServerEntityDamage>)evt).Data); break;
            case WorldEventType.PlayerReleaseSkill: OnExecutePlayerReleaseSkillEvent?.Invoke(((WorldEvent<ServerPlayerReleaseSkill>)evt).Data); break;
            case WorldEventType.EntityReleaseSkill: OnExecuteEntityReleaseSkillEvent?.Invoke(((WorldEvent<ServerEntityReleaseSkill>)evt).Data); break;
            case WorldEventType.PlayerMove: OnExecutePlayerMoveEvent?.Invoke(((WorldEvent<ServerPlayerMoveSync>)evt).Data); break;
            case WorldEventType.EntityMove: OnExecuteEntityMoveEvent?.Invoke(((WorldEvent<ServerEntityMoveSync>)evt).Data); break;
            
        }
    }

    public event Action<ServerEntitySpawn> OnExecuteEntitySpawnEvent;
    public event Action<ServerEntityDespawn> OnExecuteEntityDespawnEvent;
    public event Action<ServerEntityDamage> OnExecuteEntityDamageEvent;
    public event Action<ServerEntityReleaseSkill> OnExecuteEntityReleaseSkillEvent;
    public event Action<ServerPlayerMoveSync> OnExecutePlayerMoveEvent;
    public event Action<ServerEntityMoveSync> OnExecuteEntityMoveEvent;
    public event Action<ServerPlayerReleaseSkill> OnExecutePlayerReleaseSkillEvent;
    public event Action<ServerSkillTimelineEventMove> OnExecuteSkillTimelineEvent;
    public event Action<ServerAddItem> OnAddItemEvent;
    
    
    public event Action<ResponseMessage<NetworkPlayer>> OnLoginResponseEvent;
    public event Action<ResponseMessage<string>> OnRegisterResponseEvent;

    public event Action<ResponseMessage<List<NetworkCharacter>>> OnCreateCharacterResponseEvent;
    public event Action<ServerPlayerEnterGame> OnEnterGameResponseEvent;
    public event Action<ServerSwapStorageSlotResponse> OnSwapInventorySlotResponseEvent;
    public event Action<ServerQueryInventory> OnQueryInventoryEvent;
    
    public event Action<ServerCreateDungeonTeam>  OnCreateDungeonTeamEvent;
    public event Action<ServerPlayerEnterDungeon>  OnPlayerEnterDungeonEvent;

    public event Action<ServerDungeonTeamInvite>  OnTeamInvitePlayerEvent;
    public event Action<ServerPlayerEnterTeam>  OnPlayerEnterTeamEvent;

    public event Action<ChatMessageData> OnChatMessageEvent;
    
    public event Action<ServerAddFriend> OnAddFriendEvent;
    public event Action<NetworkFriendRequestData> OnAddFriendRequestEvent;
    public event Action<NetworkFriendData> OnHandleFriendRequestEvent;
    public event Action<NetworkFriendGroupData> OnAddFriendGroupEvent;
    public event Action<ServerFriendListSync> OnFriendListSyncEvent;
    
    public event Action<ServerLoadDungeon> OnLoadDungeonEvent;
    public event Action<ServerLevelRegion> OnLevelRegionEvent;
    public event Action<ServerLevelDungeon>  OnLevelDungeonEvent;

}


