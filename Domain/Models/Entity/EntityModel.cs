using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 实体管理数据模型
/// 负责管理游戏中所有实体的创建、销毁和状态管理
/// </summary>
public class EntityModel :  IDisposable
{
    private readonly EntityPrefabConfig prefabConfig = ResourceService.Instance.LoadResource<EntityPrefabConfig>("Data/EntityPrefabConfig");
    private readonly Dictionary<int, EntityBase> entities = new Dictionary<int, EntityBase>();
    private LocalRoleEntity localEntity;
    
    public event Action<int> OnEntityCreated;
    
    public event Action<int> OnEntityDestroyed;
    public event Action<int, float, float, EntityType> OnEntityHpUpdated;
    public event Action<Vector3, float> OnEntityHit;
    public LocalRoleEntity LocalEntity => localEntity;
    
    /// <summary>
    /// 创建实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="networkEntity">网络实体数据</param>
    /// <param name="isLocal">是否为本地玩家</param>
    /// <returns>创建的实体对象</returns>
    public T CreateEntity<T>(NetworkEntity networkEntity, bool isLocal = false) where T : EntityBase
    {
        if (entities.TryGetValue(networkEntity.EntityId, out var existed))
        {
            return existed as T;
        }
        
        var prefab = prefabConfig?.GetPrefab(networkEntity.EntityType, isLocal);
        if (prefab == null)
        {
            Debug.LogError($"缺少预制体:{networkEntity.EntityType}/{isLocal}");
            return null;
        }

        var go = UnityEngine.Object.Instantiate(prefab, networkEntity.Position, Quaternion.Euler(0, networkEntity.Yaw, 0));
        var entity = go.GetComponent<T>();
        entity.NetworkEntity = networkEntity;

        if (isLocal)
        {
            localEntity = entity as LocalRoleEntity;
        }

        entity.Initialize(networkEntity.EntityId, networkEntity.EntityType, isLocal);
        entities.Add(networkEntity.EntityId, entity);
        OnEntityCreated?.Invoke(entity.EntityId);

        return entity;
    }

    /// <summary>
    /// 尝试获取实体
    /// </summary>
    public bool TryGetEntity(int entityId, out EntityBase entity)
    {
        return entities.TryGetValue(entityId, out entity);
    }

    /// <summary>
    /// 尝试获取指定类型的实体
    /// </summary>
    public bool TryGetEntity<T>(int entityId, out T entity) where T : EntityBase
    {
        if (entities.TryGetValue(entityId, out var value) && value is T)
        {
            entity = (T)value;
            return entity != null;
        }

        entity = null;
        return false;
    }

    /// <summary>
    /// 获取所有实体
    /// </summary>
    public IEnumerable<EntityBase> GetAllEntities()
    {
        return entities.Values;
    }

    /// <summary>
    /// 移除实体
    /// </summary>
    public bool RemoveEntity(int entityId)
    {
        if (!entities.TryGetValue(entityId, out var entity))
        {
            return true;
        }

        Object.Destroy(entity.gameObject);
        entities.Remove(entityId);

        OnEntityDestroyed?.Invoke(entityId);

        return true;
    }

    /// <summary>
    /// 清空所有实体
    /// </summary>
    public void ClearAllEntities()
    {
        entities.Clear();
        localEntity = null;
    }

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    public bool ContainsEntity(int entityId)
    {
        return entities.ContainsKey(entityId);
    }

    public void Dispose()
    {
        
    }

    public bool IsLocalEntity(int entityId)
    {
        return localEntity.EntityId == entityId;
    }

    public void UpdateCurrentHp(float currentHp, int entityId)
    {
        if (!entities.TryGetValue(entityId, out var entity)) return;
        switch (entity.NetworkEntity)
        {
            case NetworkCharacter character:
                character.Hp = currentHp;
                OnEntityHpUpdated?.Invoke(entity.EntityId, currentHp, character.MaxHp, character.EntityType);
                break;
            case NetworkMonster monster:
                monster.Hp = currentHp;
                OnEntityHpUpdated?.Invoke(entity.EntityId, currentHp, monster.MaxHp, monster.EntityType);
                break;
        }
    
        
    }

    public void EntityHit(int entityId, float damage)
    {
        if (!entities.TryGetValue(entityId, out var entity)) return;
        OnEntityHit?.Invoke(entity.transform.position, damage);
    }
}

