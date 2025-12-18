using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public interface IPooledObject
{
    void OnObjectSpawn();
    void OnObjectDespawn();
}

/// <summary>
/// 修复后的对象池管理器
/// 解决了原版本中Despawn方法的问题
/// </summary>
public class PoolService : SingletonMono<PoolService>
{
    [Header("自动清理设置")]
    [SerializeField] private bool enableAutoCleanup = true;
    [SerializeField] private float cleanupThreshold = 300f; // 5分钟阈值
    [SerializeField] private int minKeepCount = 5;
    [SerializeField] private int maxCleanupPerFrame = 10; // 每帧最多清理数量
    [SerializeField] private float cleanupCheckInterval = 60f; // 检查间隔(秒)
    
    private float lastCleanupCheckTime;
    private readonly List<GameObject> pendingDestroyList = new List<GameObject>();
    private bool isCleaningUp = false;
    
    private readonly Dictionary<int, Queue<GameObject>> poolDictionary = new();
    private readonly Dictionary<int, Transform> parentTransforms = new();
    private readonly Dictionary<int, GameObject> prefabDictionary = new();
    private readonly Dictionary<int, float> lastReturnTimeDict = new(); // 记录每个池最近归还时间
    
    private Coroutine cleanupCoroutine;

    
    protected override void Awake()
    {
        base.Awake();
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }
    
    protected override void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        base.OnDestroy();
 
    }
    
    private void OnSceneUnloaded(Scene scene)
    {
        ClearAllPools(); 
    }
    
    private void Start()
    {
        if (enableAutoCleanup)
        {
            cleanupCoroutine = StartCoroutine(CleanupRoutine());
        }
    }

    private IEnumerator CleanupRoutine()
    {
        var wait = new WaitForSeconds(cleanupCheckInterval);
        while (enableAutoCleanup)
        {
            yield return wait;
            yield return StartCoroutine(CollectIdleObjects());
            yield return StartCoroutine(DestroyPendingObjects());
        }
    }

    // 第一步：收集真正需要销毁的对象（分帧）
    private IEnumerator CollectIdleObjects()
    {
        pendingDestroyList.Clear();
        float now = Time.time;

        foreach (var kvp in poolDictionary)
        {
            int poolKey = kvp.Key;
            var queue = kvp.Value;
            if (queue.Count <= minKeepCount) continue;

            var tempList = new List<GameObject>();
            int processed = 0;

            while (queue.Count > 0)
            {
                GameObject obj = queue.Dequeue();
                var identifier = obj.GetComponent<PooledObjectIdentifier>();

                // 判断是否真的空闲很久
                if (identifier != null && 
                    now - identifier.LastReturnTime > cleanupThreshold &&
                    queue.Count + tempList.Count > minKeepCount) // 保证不会低于保底
                {
                    pendingDestroyList.Add(obj);
                }
                else
                {
                    tempList.Add(obj);
                }

                processed++;
                if (processed >= 10)
                {
                    processed = 0;
                    yield return null;
                }
            }

            // 重建队列
            foreach (var obj in tempList)
                queue.Enqueue(obj);
        }
    }

    // 第二步：真正销毁（严格控制每帧数量，防止卡顿）
    private IEnumerator DestroyPendingObjects()
    {
        int destroyedThisFrame = 0;
        for (int i = pendingDestroyList.Count - 1; i >= 0 && destroyedThisFrame < maxCleanupPerFrame; i--)
        {
            var obj = pendingDestroyList[i];
            if (obj != null)
            {
                pendingDestroyList.RemoveAt(i);
                Destroy(obj);
                destroyedThisFrame++;
            }
        }

        if (pendingDestroyList.Count > 0)
            yield return null; // 还有剩余，下帧继续
    }
    
    /// <summary>
    /// 预加载对象池
    /// </summary>
    /// <param name="prefab">预制件</param>
    /// <param name="size">预加载数量</param>
    /// <param name="callback">预加载回调</param>
    public void Preload(GameObject prefab, int size, Action<GameObject> callback = null)
    {
        int poolKey = prefab.GetInstanceID();
        if (!poolDictionary.ContainsKey(poolKey))
        {
            GameObject poolParent = new GameObject($"Pool_{prefab.name}");
            poolParent.transform.SetParent(transform, false);
            
            Queue<GameObject> objectPool = new Queue<GameObject>();
            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab, poolParent.transform, false);
                obj.SetActive(false);
                
                // 为对象添加池键标识
                PooledObjectIdentifier identifier = obj.GetComponent<PooledObjectIdentifier>();
                if (identifier == null)
                {
                    identifier = obj.AddComponent<PooledObjectIdentifier>();
                }
                identifier.PoolKey = poolKey;
                
                objectPool.Enqueue(obj);
                
                callback?.Invoke(obj);
            }

            poolDictionary.Add(poolKey, objectPool);
            parentTransforms.Add(poolKey, poolParent.transform);
            prefabDictionary.Add(poolKey, prefab);
            
        }
    }

    /// <summary>
    /// 从对象池获取对象
    /// </summary>
    /// <param name="prefab">预制件</param>
    /// <param name="parent">父物体</param>
    /// <param name="worldPositionStays">是否保留世界位置</param>
    /// <returns>对象实例</returns>
    public GameObject Spawn(GameObject prefab, Transform parent = null, bool worldPositionStays = false)
    {
        Vector3 pos = parent ? parent.position : prefab.transform.position;
        Quaternion rot = parent ? parent.rotation : prefab.transform.rotation;
        return Spawn(prefab, pos, rot, parent);
    }
    
    /// <summary>
    /// 从对象池获取对象并设置位置和旋转
    /// </summary>
    /// <param name="prefab">预制件</param>
    /// <param name="position">位置</param>
    /// <param name="rotation">旋转</param>
    /// <param name="parent">父物体</param>
    /// <returns>对象实例</returns>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) throw new ArgumentNullException(nameof(prefab));

        int key = prefab.GetInstanceID();

        if (!poolDictionary.ContainsKey(key))
            Preload(prefab, 1);

        var pool = poolDictionary[key];

        if (pool.Count == 0)
            ExpandPool(key, Mathf.Max(5, pool.Count + 5));

        GameObject obj = pool.Dequeue();
        var trans = obj.transform;

        // ✅ 只有在 parent 真的变了的时候才 SetParent
        if (parent != null && trans.parent != parent)
        {
            trans.SetParent(parent, false);
        }

        trans.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        var id = obj.GetComponent<PooledObjectIdentifier>() ?? obj.AddComponent<PooledObjectIdentifier>();
        id.PoolKey = key;
        id.LastReturnTime = Time.time;
        id.IsInUse = true;

        foreach (var pooled in obj.GetComponentsInChildren<IPooledObject>(true))
            pooled.OnObjectSpawn();

        return obj;
    }
    
    /// <summary>
    /// 回收对象到对象池
    /// </summary>
    /// <param name="obj">要回收的对象</param>
    /// <param name="moveToPoolParent">是否需要放入对象池根对象下</param>
    public void Despawn(GameObject obj, bool moveToPoolParent = true)
    {
        if (obj == null) return;

        var identifier = obj.GetComponent<PooledObjectIdentifier>();
        if (identifier == null || !poolDictionary.TryGetValue(identifier.PoolKey, out var pool))
        {
            Debug.LogWarning($"[PoolService] {obj.name} 不属于任何对象池，直接销毁", obj);
            Destroy(obj);
            return;
        }

        obj.SetActive(false);

        // ✅ 只有需要时才改父节点
        if (moveToPoolParent && parentTransforms.TryGetValue(identifier.PoolKey, out var poolParent))
        {
            obj.transform.SetParent(poolParent, false);
        }

        foreach (var pooled in obj.GetComponentsInChildren<IPooledObject>(true))
            pooled.OnObjectDespawn();

        identifier.LastReturnTime = Time.time;
        identifier.IsInUse = false;

        pool.Enqueue(obj);
    }
    
    /// <summary>
    /// 扩展对象池
    /// </summary>
    /// <param name="poolKey">池键</param>
    /// <param name="expandSize">扩展数量</param>
    /// <param name="onCreated">创建回调</param>
    private void ExpandPool(int poolKey, int expandSize, Action<GameObject> onCreated = null)
    {
        if (!prefabDictionary.TryGetValue(poolKey, out GameObject prefab)) return;
        if (!parentTransforms.TryGetValue(poolKey, out Transform parent)) return;
        var queue = poolDictionary[poolKey];
        for (int i = 0; i < expandSize; i++)
        {
            GameObject obj = Instantiate(prefab, parent, false);
            obj.SetActive(false);
            
            // 为新对象添加池键标识
            PooledObjectIdentifier identifier = obj.GetComponent<PooledObjectIdentifier>();
            if (identifier == null)
            {
                identifier = obj.AddComponent<PooledObjectIdentifier>();
            }
            identifier.PoolKey = poolKey;
            
            queue.Enqueue(obj);
        }

        Debug.Log($"Expanded pool {poolKey} by {expandSize} objects");
    }
    
    /// <summary>
    /// 清空所有对象池
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var pool in poolDictionary)
        {
            while (pool.Value.Count > 0)
            {
                GameObject obj = pool.Value.Dequeue();
                if (obj != null) Destroy(obj);
            }
        }

        poolDictionary.Clear();
        parentTransforms.Clear();
        prefabDictionary.Clear();
    }

    /// <summary>
    /// 获取对象池统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public string GetPoolStats()
    {
        var stats = new System.Text.StringBuilder();
        stats.AppendLine("Object Pool Statistics:");
        
        foreach (var pool in poolDictionary)
        {
            int poolKey = pool.Key;
            int availableCount = pool.Value.Count;
            string prefabName = prefabDictionary.ContainsKey(poolKey) ? 
                               prefabDictionary[poolKey].name : "Unknown";
            
            stats.AppendLine($"  {prefabName}: {availableCount} available");
        }
        
        return stats.ToString();
    }

    /// <summary>
    /// 预热指定对象池
    /// </summary>
    /// <param name="prefab">预制件</param>
    /// <param name="targetSize">目标大小</param>
    public void WarmupPool(GameObject prefab, int targetSize)
    {
        int key = prefab.GetInstanceID();
        if (!poolDictionary.TryGetValue(key, out var queue))
        {
            Preload(prefab, targetSize);
            return;
        }

        if (queue.Count < targetSize)
            ExpandPool(key, targetSize - queue.Count);
    }

    #region Unity Editor 调试

#if UNITY_EDITOR
    [ContextMenu("Print Pool Stats")]
    private void PrintPoolStats()
    {
        Debug.Log(GetPoolStats());
    }

    [ContextMenu("Clear All Pools")]
    private void DebugClearAllPools()
    {
        ClearAllPools();
        Debug.Log("All pools cleared");
    }
#endif

    #endregion
}

/// <summary>
/// 池对象标识符组件
/// 用于标识对象属于哪个对象池
/// </summary>
public class PooledObjectIdentifier : MonoBehaviour
{
    /// <summary>
    /// 对象池键
    /// </summary>
    public int PoolKey { get; set; }
    public float LastReturnTime { get; set; }
    public bool IsInUse { get; set; }
}

