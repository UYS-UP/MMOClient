using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Snapshot
{
    public int Tick;
    public Vector3 Pos;
    public float Yaw;
    public float Speed;
    public Vector3 Dir;
    public MotionStateType MotionState;
    public ActionStateType ActionState;
}

public abstract class EntityBase : MonoBehaviour
{
    [SerializeField] private string entityId;
    [SerializeField] private EntityType entityType;
    [SerializeField] private bool isLocal;

    private readonly Dictionary<Type, BaseComponent> components = new();
    
    public NetworkEntity NetworkEntity { get; set; }
    public Snapshot CurrentSnapshot { get; set; }
    public EntityHFSM FSM { get; private set; }
    public string EntityId => entityId;
    public EntityType EntityType => entityType;
    public bool IsLocal => isLocal;


    public bool TryGetNetworkEntity<T>(out T entity) where T : NetworkEntity
    {
        if (NetworkEntity is T)
        {
            entity = (T)NetworkEntity;
            return entity != null;
        }
        entity = null;
        return false;
    }
    
    public virtual void Initialize(string id, EntityType type, bool local)
    {
        entityId = id;
        entityType = type;
        isLocal = local;

        SetupComponents();
        foreach (var component in components.Values)
        {
            component.Attach(this);
        }
        FSM = new EntityHFSM(this);
    }

    protected abstract void SetupComponents();
    
    public void UpdateEntity()
    {
        float dt = Time.deltaTime;
        foreach (var c in components.Values)
            c.UpdateEntity(dt);
        FSM.Update(dt);
    }
    
    public void LateUpdateEntity()
    {
        float dt = Time.deltaTime;
        foreach (var c in components.Values)
            c.LateUpdateEntity(dt);
    }

    private void OnAnimatorMove()
    {
        foreach (var c in components.Values)
        {
            c.OnAnimatorMove();
        }
    }
    

    protected void AddEntityComponent<T>(T c) where T : BaseComponent
    {
        var key = typeof(T);
        if (components.ContainsKey(key))
            Debug.LogWarning($"重复添加实体组件 {key.Name} 到实体 {entityId}");
        components[key] = c;
    }

    public T GetEntityComponent<T>() where T : BaseComponent =>
        components.TryGetValue(typeof(T), out var c) ? (T)c : null;

    protected virtual void OnDestroy()
    {
        foreach (var controller in components)
        {
            controller.Value.ClearComponent();
        }
        components.Clear();
    }

    
    

}
