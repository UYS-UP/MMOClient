using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;



/// <summary>
/// 实体表现层
/// </summary>
public partial class EntityWorld : MonoBehaviour
{
    private EntityModel entityModel;
    private PlayerModel playerModel;
    private Queue<Action> pendingActions;
    
    
    
    private void Awake()
    {
        entityModel = GameContext.Instance.Get<EntityModel>();
        playerModel = GameContext.Instance.Get<PlayerModel>();
        pendingActions = new Queue<Action>();
        TickProtocolRegister();
    }
    
    private void Update()
    {
        while (pendingActions.Count > 0)
        {
            pendingActions.Dequeue()?.Invoke();
        }
        
        ProcessWorldEvents();
        
        foreach (var e in entityModel.GetAllEntities())
        {
            e.UpdateEntity();
        }
    }

    private void LateUpdate()
    {
        foreach (var e in entityModel.GetAllEntities()) e.LateUpdateEntity();
    }
    
    # region 实体生成销毁
    private void OnExecuteEntitySpawnEvent(ServerEntitySpawn data)
    {
        if (entityModel.ContainsEntity(data.SpawnEntity.EntityId)) return;
        if (data.SpawnEntity.EntityType == EntityType.Monster)
        {
            entityModel.CreateEntity<RemoteMonsterEntity>(data.SpawnEntity);
        }else if (data.SpawnEntity.EntityType == EntityType.Character)
        {
            if (data.SpawnEntity is NetworkCharacter character && entityModel.CharacterId == character.CharacterId)
            {
                entityModel.CreateEntity<LocalRoleEntity>(character, true);
                return;
            }
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
    private void OnExecuteEntityReleaseSkillEvent(ServerEntityCastSkill data)
    {
        if(!entityModel.TryGetEntity(data.Caster, out var e)) return;
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
            e.FSM.Ctx.RequestHit();
            entityModel.EntityHit(e.EntityId, wound.Wound);
            entityModel.UpdateCurrentHp(wound.CurrentHp, wound.Target);
        }

        foreach (var death in data.Deaths)
        {
            if(!entityModel.TryGetEntity(death.Target, out var e)) return;
            e.FSM.Ctx.RequestDeath();
            entityModel.EntityHit(e.EntityId, death.Wound);
            entityModel.UpdateCurrentHp(0, death.Target);
            Debug.Log($"怪物死亡, 掉落物数量: {death.DroppedItems.Count}");
            if (death.DroppedItems.Count > 0)
            {
                
            }
        }
    }
    #endregion
    
    private void OnDestroy()
    {
        EventService.Instance.Unsubscribe(this);
        TickProtocolUnregister();
        entityModel.ClearAllEntities();
    }

}
