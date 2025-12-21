using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

/// <summary>
/// 实体表现层
/// </summary>
public class EntityWorld : MonoBehaviour
{
    private ProtocolRegister protocolRegister;
    private EntityModel entityModel;
    
    private Queue<Action> pendingActions;
    
    
    
    private void Awake()
    {
        protocolRegister = ProtocolRegister.Instance;
        entityModel = GameContext.Instance.Get<EntityModel>();
        
        pendingActions = new Queue<Action>();


        protocolRegister.OnExecuteEntitySpawnEvent += OnExecuteEntitySpawnEvent;
        protocolRegister.OnExecuteEntityDespawnEvent += OnExecuteEntityDespawnEvent;
        protocolRegister.OnExecuteEntityDamageEvent += OnExecuteEntityDamageEvent;
        protocolRegister.OnExecuteEntityReleaseSkillEvent += OnExecuteEntityReleaseSkillEvent;
        protocolRegister.OnExecutePlayerMoveEvent += OnExecutePlayerMoveEvent;
        protocolRegister.OnExecuteEntityMoveEvent += OnExecuteEntityMoveEvent;
        protocolRegister.OnExecutePlayerReleaseSkillEvent += OnExecutePlayerReleaseSkillEvent;
        protocolRegister.OnExecuteSkillTimelineEvent += OnExecuteSkillTimelineEvent;
        protocolRegister.OnEnterGameResponseEvent += OnEnterGameResponseEvent;
        protocolRegister.OnPlayerEnterDungeonEvent += OnPlayerEnterDungeonEvent;
        
    }


    private void Update()
    {
        // 先处理 pendingActions
        while (pendingActions.Count > 0)
        {
            pendingActions.Dequeue()?.Invoke();
        }
        
        double renderServerTick = TickService.Instance.RenderTick;
        ProtocolRegister.Instance.ProcessWorldEvents(renderServerTick);
    
        // 更新实体（在事件执行后）
        foreach (var e in entityModel.GetAllEntities())
        {
            e.UpdateEntity();
        }
    }

    private void LateUpdate()
    {
        foreach (var e in entityModel.GetAllEntities()) e.LateUpdateEntity();
    }

    private void OnEnterGameResponseEvent(ServerPlayerEnterGame data)
    {
        OnPlayerEnterSceneEvent(data.PlayerEntity);
    }

    private void OnPlayerEnterDungeonEvent(ServerPlayerEnterDungeon data)
    {
        OnPlayerEnterSceneEvent(data.PlayerEntity);
    }
    
    private void OnPlayerEnterSceneEvent(NetworkEntity entity)
    {
        pendingActions.Enqueue(() =>
        {
            entityModel.CreateEntity<LocalRoleEntity>(entity, true);
        });
        
    }
    
    # region 实体物品生成销毁
    private void OnExecuteEntitySpawnEvent(ServerEntitySpawn data)
    {
        if (entityModel.ContainsEntity(data.SpawnEntity.EntityId)) return;
        if (data.SpawnEntity.EntityType == EntityType.Monster)
        {
            entityModel.CreateEntity<RemoteMonsterEntity>(data.SpawnEntity);
        }else if (data.SpawnEntity.EntityType == EntityType.Character)
        {
           entityModel.CreateEntity<RemoteRoleEntity>(data.SpawnEntity);
        }else if (data.SpawnEntity.EntityType == EntityType.Npc)
        {
            entityModel.CreateEntity<RemoteNpcEntity>(data.SpawnEntity);
        }
        
        
    }
    

    private void OnExecuteEntityDespawnEvent(ServerEntityDespawn data)
    {
        foreach (var id in data.DespawnEntities)
            entityModel.RemoveEntity(id);
    }
    
    # endregion
    
    # region 移动同步
    private void OnExecutePlayerMoveEvent(ServerPlayerMoveSync data)
    {
        if (!entityModel.TryGetEntity(data.EntityId, out var e)) return;
        e.NetworkEntity.Position = HelperUtility.ShortArrayToVector3(data.Position);
        e.NetworkEntity.Yaw = HelperUtility.ShortToYaw(data.Yaw);
        e.NetworkEntity.Direction = HelperUtility.SbyteArrayToVector3(data.Direction);
        e.NetworkEntity.Speed = data.Speed;
        
        e.GetEntityComponent<LocalMoveComponent>().ReconcileTo(data.IsValid, data.ClientTick);
    }
    
    private void OnExecuteEntityMoveEvent(ServerEntityMoveSync data)
    {
        if (!entityModel.TryGetEntity(data.EntityId, out var e))
        {
            return;
        }

        e.NetworkEntity.Position = HelperUtility.ShortArrayToVector3(data.Position);
        e.NetworkEntity.Yaw = HelperUtility.ShortToYaw(data.Yaw);
        e.NetworkEntity.Direction = HelperUtility.SbyteArrayToVector3(data.Direction);
        e.NetworkEntity.State = data.State;
        e.NetworkEntity.Speed = data.Speed;
        
        e.GetEntityComponent<RemoteMoveComponent>()
            .OnNetUpdate(data.Tick);
    }
    #endregion
    
    # region 战斗相关
    private void OnExecuteEntityReleaseSkillEvent(ServerEntityReleaseSkill data)
    {
        if(!entityModel.TryGetEntity(data.ReleaserId, out var e)) return;
        // 先更新位置和方向
        e.NetworkEntity.Position = HelperUtility.ShortArrayToVector3(data.Position);
        e.NetworkEntity.Yaw = HelperUtility.ShortToYaw(data.Yaw);
        e.NetworkEntity.Direction = Vector3.zero;
    
        e.GetEntityComponent<RemoteMoveComponent>().OnNetUpdate(data.Tick);
        Debug.Log(e.EntityId);
        e.GetEntityComponent<RemoteSkillComponent>().CastSkill(data.SkillId);
    }

    private void OnExecutePlayerReleaseSkillEvent(ServerPlayerReleaseSkill data)
    {


        
    }

    private void OnExecuteSkillTimelineEvent(ServerSkillTimelineEventMove data)
    {
        if(!entityModel.TryGetEntity(data.EntityId, out var e)) return;
        Debug.Log("执行TimelineEvent");
        e.NetworkEntity.Position = data.EndPos;
        StartCoroutine(MoveLerpRoutine(e, data.EndPos, data.Time));
    }

    private static IEnumerator MoveLerpRoutine(EntityBase entity, Vector3 target, float duration)
    {
        Vector3 start = entity.transform.position;
        float timer = 0f;
        while (timer < duration)
        {
            
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            entity.transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
        entity.transform.position = target;
    }
    
    private void OnExecuteEntityDamageEvent(ServerEntityDamage data)
    {
        
        foreach (var wound in data.Wounds)
        {
            if(!entityModel.TryGetEntity(wound.Target, out var e)) return;
            // e.GetEntityComponent<LocalSkillComponent>().Interrupt();
            e.FSM.Ctx.RequestHit();
            entityModel.UpdateCurrentHp(wound.CurrentHp, wound.Target);
        }

        foreach (var death in data.Deaths)
        {
            if(!entityModel.TryGetEntity(death.Target, out var e)) return;
            // e.GetEntityComponent<LocalSkillComponent>().Interrupt();
            e.FSM.Ctx.RequestDeath();
            entityModel.UpdateCurrentHp(0, death.Target);
            Debug.Log($"怪物死亡, 掉落物数量: {death.DroppedItems.Count}");
            if (death.DroppedItems.Count > 0)
            {
                
            }
        }
    }
    

    private void OnDestroy()
    {
        protocolRegister.OnExecuteEntitySpawnEvent -= OnExecuteEntitySpawnEvent;
        protocolRegister.OnExecuteEntityDespawnEvent -= OnExecuteEntityDespawnEvent;
        protocolRegister.OnExecuteEntityDamageEvent -= OnExecuteEntityDamageEvent;
        protocolRegister.OnExecuteEntityReleaseSkillEvent -= OnExecuteEntityReleaseSkillEvent;
        protocolRegister.OnExecutePlayerMoveEvent -= OnExecutePlayerMoveEvent;
        protocolRegister.OnExecuteEntityMoveEvent -= OnExecuteEntityMoveEvent;
        EventService.Instance.Unsubscribe(this);
        entityModel.ClearAllEntities();
    }

    #endregion

}
