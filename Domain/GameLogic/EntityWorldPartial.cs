

using System.Collections.Generic;
using UnityEngine;

public enum WorldEventType
{
    EntitySpawn,
    EntityDespawn,
    EntityMove,
    EntityReleaseSkill,
    MonsterDeath,
    EntityHit,
    PlayerMove,
    PlayerReleaseSkill,
    DropItemSpawn,
    DropItemDespawn,
    SkillTimelineEvent
}

public interface IWorldEvent
{
    int Tick { get; }
    WorldEventType Type { get; }
    
}

public struct WorldEvent<T> : IWorldEvent where T : TickMessage
{
    public int Tick { get; set; }
    public WorldEventType Type { get; set; }
    public T Data { get; set; }
}

public partial class EntityWorld
{
    private MinHeap<IWorldEvent> worldEvents = new MinHeap<IWorldEvent>((a, b) => a.Tick.CompareTo(b.Tick));
    
    private void TickProtocolRegister()
    {
        GameClient.Instance.RegisterHandler(Protocol.SC_EntitySpawn, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.SC_EntityDespawn, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.SC_EntityMove, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.SC_CharacterMove, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.SC_EntityDeath, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.SC_EntityCastSkill, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.SC_EntityDamage, ExecuteNetworkEvent);
        GameClient.Instance.RegisterHandler(Protocol.SC_CharacterCastSkill, ExecuteNetworkEvent);
    }
    
    private void TickProtocolUnregister()
    {
        GameClient.Instance.UnregisterHandler(Protocol.SC_EntitySpawn);
        GameClient.Instance.UnregisterHandler(Protocol.SC_EntityDespawn);
        GameClient.Instance.UnregisterHandler(Protocol.SC_EntityMove);
        GameClient.Instance.UnregisterHandler(Protocol.SC_CharacterMove);
        GameClient.Instance.UnregisterHandler(Protocol.SC_EntityDeath);
        GameClient.Instance.UnregisterHandler(Protocol.SC_EntityCastSkill);
        GameClient.Instance.UnregisterHandler(Protocol.SC_EntityDamage);
        GameClient.Instance.UnregisterHandler(Protocol.SC_CharacterCastSkill);
    }
    
    public void ProcessWorldEvents() 
    {
        int maxEventsPerFrame = 200;
        int processed = 0;
        var renderServerTickExact = TickService.Instance.RenderTick;

        while (worldEvents.Count > 0 && processed < maxEventsPerFrame)
        {
            if (!worldEvents.Peek(out var evt)) break;
            
            if (evt.Tick > renderServerTickExact) break;
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
            case Protocol.SC_EntitySpawn:
                var entitySpawn = packet.DeSerializePayload<ServerEntitySpawn>();
                worldEvents.Push(new WorldEvent<ServerEntitySpawn> { Tick = entitySpawn.Tick, Type = WorldEventType.EntitySpawn, Data = entitySpawn });
                break;
            case Protocol.SC_EntityDespawn:
                var entityDespawn = packet.DeSerializePayload<ServerEntityDespawn>();
                worldEvents.Push(new WorldEvent<ServerEntityDespawn> { Tick = entityDespawn.Tick, Type = WorldEventType.EntityDespawn, Data = entityDespawn });
                break;
            case Protocol.SC_EntityMove:
                var entityMove = packet.DeSerializePayload<ServerEntityMoveSync>();
                worldEvents.Push(new WorldEvent<ServerEntityMoveSync> { Tick = entityMove.Tick, Type = WorldEventType.EntityMove, Data = entityMove });
                break;
            case Protocol.SC_CharacterCastSkill:
                var playerReleaseSkill = packet.DeSerializePayload<ServerPlayerReleaseSkill>();
                worldEvents.Push(new WorldEvent<ServerPlayerReleaseSkill> { Tick = playerReleaseSkill.Tick, Type = WorldEventType.PlayerReleaseSkill, Data = playerReleaseSkill });
                break;
            case Protocol.SC_EntityCastSkill:
                var entityReleaseSkill = packet.DeSerializePayload<ServerEntityCastSkill>();
                worldEvents.Push(new WorldEvent<ServerEntityCastSkill> { Tick = entityReleaseSkill.Tick, Type = WorldEventType.EntityReleaseSkill, Data = entityReleaseSkill });
                break;
            case Protocol.SC_CharacterMove:
                var playerMove = packet.DeSerializePayload<ServerPlayerMoveSync>();
                worldEvents.Push(new WorldEvent<ServerPlayerMoveSync> { Tick = playerMove.Tick, Type = WorldEventType.PlayerMove, Data = playerMove });
                break;
            case Protocol.SC_EntityDamage:
                var entityDamage = packet.DeSerializePayload<ServerEntityDamage>();
                worldEvents.Push(new WorldEvent<ServerEntityDamage> { Tick = entityDamage.Tick, Type = WorldEventType.EntityHit, Data = entityDamage });
                break;
            case Protocol.SC_EntityDeath:
                var monsterDeath = packet.DeSerializePayload<ServerMonsterDeath>();
                worldEvents.Push(new WorldEvent<ServerMonsterDeath>() { Tick = monsterDeath.Tick, Type = WorldEventType.MonsterDeath, Data = monsterDeath });
                break;
        }
    }
    
    
    private void ExecuteWorldEvent(IWorldEvent evt)
    {
        switch (evt.Type)
        {
            case WorldEventType.EntitySpawn:
                OnExecuteEntitySpawnEvent(((WorldEvent<ServerEntitySpawn>)evt).Data); 
                break;
            case WorldEventType.EntityDespawn: 
                OnExecuteEntityDespawnEvent(((WorldEvent<ServerEntityDespawn>)evt).Data); 
                break;
            case WorldEventType.EntityHit: 
                OnExecuteEntityDamageEvent(((WorldEvent<ServerEntityDamage>)evt).Data); 
                break;
            case WorldEventType.PlayerReleaseSkill: 
                OnExecutePlayerReleaseSkillEvent(((WorldEvent<ServerPlayerReleaseSkill>)evt).Data); 
                break;
            case WorldEventType.EntityReleaseSkill: 
                OnExecuteEntityReleaseSkillEvent(((WorldEvent<ServerEntityCastSkill>)evt).Data); 
                break;
            case WorldEventType.PlayerMove: 
                OnExecutePlayerMoveEvent(((WorldEvent<ServerPlayerMoveSync>)evt).Data); 
                break;
            case WorldEventType.EntityMove: 
                OnExecuteEntityMoveEvent(((WorldEvent<ServerEntityMoveSync>)evt).Data); 
                break;
            default:
                Debug.LogWarning($"[ProtocolRegister] Unknown event: {evt.Type}");
                break;
            
        }
    }
}