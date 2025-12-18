using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class InfiniteScrollView<T> : MonoBehaviour
{
    #region 配置参数
    [Header("布局设置")]
    [SerializeField] protected GameObject itemPrefab;
    [SerializeField] protected int columnCount = 1;
    [SerializeField] protected float spacingX = 5f;
    [SerializeField] protected float spacingY = 20f;
    [SerializeField] protected Vector2 padding = new Vector2(10, 10);

    [Header("高度限制")]
    [SerializeField] protected float minItemHeight = 20f;
    [SerializeField] protected float maxItemHeight = 500f;
    [SerializeField] protected bool enableHeightLimits = true;

    [Header("性能优化")]
    [SerializeField] protected int warmupCount = 10;
    [SerializeField] protected float spawnOverscanPx = 200f;
    [SerializeField] protected float despawnMarginPx = 250f;
    [SerializeField] protected float heightChangeThreshold = 5f;

    [Header("调试")]
    [SerializeField] protected bool enableDebugLog = false;
    #endregion

    #region 组件引用
    protected ScrollRect scrollRect;
    protected RectTransform viewport;
    protected RectTransform content;
    #endregion

    #region 数据层
    protected readonly List<T> dataList = new();
    protected readonly List<ItemMetadata> itemMetadata = new();
    protected Action<GameObject, T> onBindItem;
    public Func<T, GameObject> GetPrefabForItem;
    #endregion

    #region 视图层
    protected readonly Dictionary<int, GameObject> visibleItems = new();
    protected readonly List<float> columnHeights = new();
    #endregion

    #region 状态管理
    protected enum State
    {
        Uninitialized,
        Initializing,
        Ready,
        Updating
    }

    [Flags]
    protected enum DirtyFlags
    {
        None = 0,
        Content = 1 << 0,      // Content 尺寸需要更新
        Visibility = 1 << 1    // 可见性需要更新
    }

    protected State currentState = State.Uninitialized;
    protected DirtyFlags dirtyFlags = DirtyFlags.None;
    protected bool updateScheduled = false;
    private int pendingRecalcFrom = -1;
    #endregion

    #region Item 元数据
    protected class ItemMetadata
    {
        public Vector2 position;
        public float height;           // 当前使用的高度（预估或实际）
        public bool hasActualHeight;   // 是否已测量实际高度
        public int columnIndex;
    }
    #endregion

    #region 批处理优化
    private readonly List<Action> batchedActions = new List<Action>(64);
    private bool isBatchingFrame = false;
    

    #endregion

    #region 生命周期
    protected virtual void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        viewport = scrollRect.viewport;
        content = scrollRect.content;

        if (content.pivot != new Vector2(0.5f, 1f))
            content.pivot = new Vector2(0.5f, 1f);
    }

    protected virtual void OnEnable()
    {
        scrollRect.onValueChanged.AddListener(OnScrollChanged);
    }

    protected virtual void OnDisable()
    {
        scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        DespawnAll();
    }

    protected virtual void OnDestroy()
    {
        dataList.Clear();
        itemMetadata.Clear();
        visibleItems.Clear();
    }
    #endregion

    #region 公开 API
    /// <summary>
    /// 初始化滚动视图
    /// </summary>
    public virtual void Initialize(List<T> data, Action<GameObject, T> onBind)
    {
        if (currentState == State.Initializing) return;
        
        currentState = State.Initializing;
        onBindItem = onBind;

        dataList.Clear();
        dataList.AddRange(data);

        FullRebuild();
        
        currentState = State.Ready;
    }

    /// <summary>
    /// 添加单个 item（增量更新，不触发完整重建）
    /// </summary>
    public virtual void AddItem(T data)
    {
        if (currentState != State.Ready)
        {
            LogWarning("AddItem called but state is not Ready");
            return;
        }

        int newIndex = dataList.Count;
        dataList.Add(data);

        Log($"AddItem at index {newIndex}");

        // 增量添加：只计算新 item 的位置
        IncrementalAdd(newIndex, data);
    }

    /// <summary>
    /// 批量添加 items
    /// </summary>
    public virtual void AddItems(List<T> items)
    {
        if (currentState != State.Ready) return;
        
        Log($"AddItems: {items.Count} items");

        foreach (var item in items)
        {
            int newIndex = dataList.Count;
            dataList.Add(item);
            IncrementalAdd(newIndex, item);
        }
    }

    /// <summary>
    /// 删除 item
    /// </summary>
    public virtual void RemoveItem(int index)
    {
        if (index < 0 || index >= dataList.Count) return;
        if (currentState != State.Ready) return;

        Log($"RemoveItem at index {index}");

        dataList.RemoveAt(index);
        
        // 如果 item 可见，先 despawn
        if (visibleItems.ContainsKey(index))
        {
            DespawnItem(index);
        }

        // 删除元数据
        int col = itemMetadata[index].columnIndex;
        float removedHeight = itemMetadata[index].height;
        itemMetadata.RemoveAt(index);

        // 更新列高度
        columnHeights[col] -= (removedHeight + spacingY);

        // 重新计算后续 item 的位置
        RecalculateFromIndex(index);

        // 更新可见性
        MarkDirty(DirtyFlags.Content | DirtyFlags.Visibility);
    }

    /// <summary>
    /// 设置高度限制
    /// </summary>
    public virtual void SetHeightLimits(float min, float max, bool enable = true)
    {
        minItemHeight = Mathf.Max(1f, min);
        maxItemHeight = Mathf.Max(minItemHeight, max);
        enableHeightLimits = enable;

        Log($"Height limits set: min={minItemHeight}, max={maxItemHeight}, enabled={enableHeightLimits}");
    }

    /// <summary>
    /// 强制完整重建（仅在必要时使用）
    /// </summary>
    public virtual void ForceRebuild()
    {
        Log("ForceRebuild requested");
        FullRebuild();
    }

    /// <summary>
    /// 刷新指定 item
    /// </summary>
    public virtual void RefreshItem(int index)
    {
        if (index < 0 || index >= dataList.Count) return;
        
        if (visibleItems.TryGetValue(index, out GameObject go))
        {
            onBindItem?.Invoke(go, dataList[index]);
        }
    }
    #endregion

    #region 增量更新
    /// <summary>
    /// 增量添加新 item
    /// </summary>
    protected virtual void IncrementalAdd(int index, T data)
    {
        // 1. 预估高度
        float estimatedHeight = EstimateItemHeight(data);
        estimatedHeight = ClampItemHeight(estimatedHeight);

        // 2. 选择最短的列
        int col = GetShortestColumnIndex();
        float colWidth = GetColumnWidth();

        // 3. 计算位置
        float x = padding.x + col * (colWidth + spacingX);
        float y = columnHeights[col];
        Vector2 position = new Vector2(x, -y);

        // 4. 创建元数据
        ItemMetadata metadata = new ItemMetadata
        {
            position = position,
            height = estimatedHeight,
            hasActualHeight = false,
            columnIndex = col
        };
        itemMetadata.Add(metadata);

        // 5. 更新列高度
        columnHeights[col] += estimatedHeight + spacingY;

        Log($"IncrementalAdd: index={index}, col={col}, pos={position}, height={estimatedHeight}");

        // 6. 标记需要更新
        MarkDirty(DirtyFlags.Content | DirtyFlags.Visibility);
    }

    /// <summary>
    /// 从指定索引重新计算位置
    /// </summary>
    protected virtual void RecalculateFromIndex(int startIndex)
    {
        if (startIndex >= itemMetadata.Count) return;

        Log($"RecalculateFromIndex: {startIndex}");

        // 重新计算受影响列的高度
        Dictionary<int, float> newColumnHeights = new Dictionary<int, float>();
        for (int i = 0; i < columnCount; i++)
        {
            newColumnHeights[i] = padding.y;
        }

        // 重新计算所有 item 的位置
        float colWidth = GetColumnWidth();
        
        for (int i = 0; i < itemMetadata.Count; i++)
        {
            int col = itemMetadata[i].columnIndex;
            
            if (i >= startIndex)
            {
                // 重新计算位置
                float x = padding.x + col * (colWidth + spacingX);
                float y = newColumnHeights[col];
                itemMetadata[i].position = new Vector2(x, -y);

                // 如果 item 可见，更新其位置
                if (visibleItems.TryGetValue(i, out GameObject go))
                {
                    RectTransform rt = (RectTransform)go.transform;
                    rt.anchoredPosition = itemMetadata[i].position;
                }
            }
            
            newColumnHeights[col] += itemMetadata[i].height + spacingY;
        }

        // 更新列高度
        for (int i = 0; i < columnCount; i++)
        {
            if (i < columnHeights.Count)
                columnHeights[i] = newColumnHeights[i];
        }
    }
    #endregion

    #region 布局管理
    /// <summary>
    /// 完整重建（仅在初始化或必要时调用）
    /// </summary>
    protected virtual void FullRebuild()
    {
        Log("FullRebuild started");

        // 1. 清理现有状态
        DespawnAll();
        itemMetadata.Clear();
        columnHeights.Clear();

        // 2. 初始化列高度
        for (int i = 0; i < Mathf.Max(1, columnCount); i++)
        {
            columnHeights.Add(padding.y);
        }

        // 3. 计算所有 item 的位置
        float colWidth = GetColumnWidth();

        for (int i = 0; i < dataList.Count; i++)
        {
            float h = EstimateItemHeight(dataList[i]);
            h = ClampItemHeight(h);

            int col = GetShortestColumnIndex();
            float x = padding.x + col * (colWidth + spacingX);
            float y = columnHeights[col];

            ItemMetadata metadata = new ItemMetadata
            {
                position = new Vector2(x, -y),
                height = h,
                hasActualHeight = false,
                columnIndex = col
            };
            itemMetadata.Add(metadata);

            columnHeights[col] += h + spacingY;
        }

        // 4. 更新 content 尺寸
        UpdateContentSize();

        // 5. 等待下一帧后更新可见性
        StartCoroutine(DelayedVisibilityUpdate());

        Log("FullRebuild completed");
    }

    private IEnumerator DelayedVisibilityUpdate()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        UpdateVisibleRange();
    }

    /// <summary>
    /// 更新 content 尺寸
    /// </summary>
    protected virtual void UpdateContentSize()
    {
        float totalHeight = padding.y;
        foreach (float h in columnHeights)
        {
            totalHeight = Mathf.Max(totalHeight, h);
        }

        content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight + padding.y);
        
        Log($"UpdateContentSize: {totalHeight + padding.y}");
    }
    #endregion

    #region 可见性管理
    protected virtual void OnScrollChanged(Vector2 _)
    {
        if (currentState != State.Ready) return;
        UpdateVisibleRange();
    }

    private float cachedViewportHeight = 0f;
    private float lastViewportHeight = 0f;
    
    /// <summary>
    /// 更新可见范围
    /// </summary>
    protected virtual void UpdateVisibleRange()
    {
        if (currentState != State.Ready) return;
        if (dataList.Count == 0)
        {
            DespawnAll();
            return;
        }
        
        if (viewport == null) return;

        float vh = viewport.rect.height;
        if (Mathf.Abs(vh - lastViewportHeight) > 1f)
        {
            lastViewportHeight = vh;
            cachedViewportHeight = vh;
        }
        else
        {
            vh = cachedViewportHeight;
        }

        float scrollY = content.anchoredPosition.y;
        float visTop = Mathf.Max(0f, scrollY - spawnOverscanPx);
        float visBot = scrollY + vh + spawnOverscanPx;

        // 计算应该可见的 item
        HashSet<int> shouldBeVisible = new HashSet<int>();
        
        for (int i = 0; i < itemMetadata.Count; i++)
        {
            float itemTop = -itemMetadata[i].position.y;
            float itemBottom = itemTop + itemMetadata[i].height;

            if (itemBottom >= visTop && itemTop <= visBot)
            {
                shouldBeVisible.Add(i);
            }
        }

        // Despawn 不再可见的 item
        float despawnTop = Mathf.Max(0f, scrollY - despawnMarginPx);
        float despawnBot = scrollY + vh + despawnMarginPx;

        List<int> toRemove = new List<int>();
        foreach (var kv in visibleItems)
        {
            int i = kv.Key;
            if (i >= itemMetadata.Count) continue;

            float itemTop = -itemMetadata[i].position.y;
            float itemBottom = itemTop + itemMetadata[i].height;
            float center = (itemTop + itemBottom) * 0.5f;

            if (center < despawnTop || center > despawnBot)
            {
                toRemove.Add(i);
            }
        }

        foreach (int i in toRemove)
        {
            DespawnItem(i);
        }

        // Spawn 新可见的 item
        foreach (int i in shouldBeVisible)
        {
            if (!visibleItems.ContainsKey(i))
            {
                SpawnItem(i);
            }
        }
    }

    /// <summary>
    /// 生成 item
    /// </summary>
    protected virtual void SpawnItem(int index)
    {
        if (index < 0 || index >= dataList.Count) return;
        if (index >= itemMetadata.Count) return;
        if (visibleItems.ContainsKey(index)) return;

        Log($"SpawnItem: {index}");
        var prefab = GetPrefabForItem?.Invoke(dataList[index]);
        GameObject go = PoolService.Instance.Spawn(prefab);
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(content, false);

        float colWidth = GetColumnWidth();
        rt.sizeDelta = new Vector2(colWidth, rt.sizeDelta.y);
        rt.anchoredPosition = itemMetadata[index].position;

        onBindItem?.Invoke(go, dataList[index]);
        visibleItems[index] = go;

        // 如果还没有实际高度，启动高度监控
        if (!itemMetadata[index].hasActualHeight)
        {
            StartCoroutine(MonitorItemHeight(index, rt));
        }
    }

    /// <summary>
    /// 回收 item
    /// </summary>
    protected virtual void DespawnItem(int index)
    {
        if (!visibleItems.TryGetValue(index, out GameObject go))
            return;

        Log($"DespawnItem: {index}");

        OnItemRecycled(go);
        PoolService.Instance.Despawn(go);
        visibleItems.Remove(index);
    }

    /// <summary>
    /// 回收所有 item
    /// </summary>
    protected virtual void DespawnAll()
    {
        Log($"DespawnAll: {visibleItems.Count} items");

        foreach (var go in visibleItems.Values)
        {
            OnItemRecycled(go);
            PoolService.Instance.Despawn(go);
        }
        visibleItems.Clear();
    }
    #endregion

    #region 高度管理
    /// <summary>
    /// 监控 item 高度变化
    /// </summary>
    protected virtual IEnumerator MonitorItemHeight(int index, RectTransform rt)
    {
        // 等待布局稳定
        yield return null;
        yield return null;

        if (index >= itemMetadata.Count) yield break;
        if (!visibleItems.ContainsKey(index)) yield break;

        float actualHeight = rt.rect.height;
        actualHeight = ClampItemHeight(actualHeight);

        float estimatedHeight = itemMetadata[index].height;
        float delta = Mathf.Abs(actualHeight - estimatedHeight);

        if (delta > heightChangeThreshold)
        {
            Log($"Height changed: index={index}, estimated={estimatedHeight:F1}, actual={actualHeight:F1}, delta={delta:F1}");
            
            // 更新高度（不触发完整重建）
            UpdateItemHeight(index, actualHeight);
        }
        else
        {
            itemMetadata[index].hasActualHeight = true;
        }
    }

    /// <summary>
    /// 更新 item 高度（局部调整）
    /// </summary>
    protected virtual void UpdateItemHeight(int index, float newHeight)
    {
        if (index >= itemMetadata.Count) return;
        float oldAnchor = CaptureAnchorY(out var anchorIndex);
        float oldHeight = itemMetadata[index].height;
        float delta = newHeight - oldHeight;

        itemMetadata[index].height = newHeight;
        itemMetadata[index].hasActualHeight = true;

        int col = itemMetadata[index].columnIndex;

        Log($"UpdateItemHeight: index={index}, col={col}, oldHeight={oldHeight:F1}, newHeight={newHeight:F1}, delta={delta:F1}");

        // 更新列高度
        columnHeights[col] += delta;

        // 调整同列后续 item 的位置
        for (int i = index + 1; i < itemMetadata.Count; i++)
        {
            if (itemMetadata[i].columnIndex == col)
            {
                itemMetadata[i].position.y -= delta;

                // 如果 item 可见，更新其位置
                if (visibleItems.TryGetValue(i, out GameObject go))
                {
                    RectTransform rt = (RectTransform)go.transform;
                    rt.anchoredPosition = itemMetadata[i].position;
                }
            }
        }

        // 更新 content 尺寸和可见性
        MarkDirty(DirtyFlags.Content | DirtyFlags.Visibility);
        
        pendingRecalcFrom = (pendingRecalcFrom < 0) ? index : Mathf.Min(pendingRecalcFrom, index);
        RestoreAnchorY(oldAnchor);
    }

    /// <summary>
    /// 限制 item 高度
    /// </summary>
    protected virtual float ClampItemHeight(float height)
    {
        if (!enableHeightLimits) return height;
        return Mathf.Clamp(height, minItemHeight, maxItemHeight);
    }

    /// <summary>
    /// 预估 item 高度
    /// </summary>
    protected virtual float EstimateItemHeight(T data)
    {
        RectTransform rt = itemPrefab != null ? itemPrefab.GetComponent<RectTransform>() : null;
        float height = rt != null ? Mathf.Max(1f, rt.sizeDelta.y) : 20f;
        return ClampItemHeight(height);
    }
    
    private float CaptureAnchorY(out int anchorIndex)
    {
        anchorIndex = -1;
        if (itemMetadata == null || itemMetadata.Count == 0 || viewport == null) return content.anchoredPosition.y;

        float scrollY = content.anchoredPosition.y;
        float viewH = viewport.rect.height;
        float anchorWorldY = scrollY + viewH * 0.5f; // 取视口中心

        float bestDist = float.MaxValue; int best = -1;
        for (int i = 0; i < itemMetadata.Count; i++)
        {
            var md = itemMetadata[i];
            float top    = -md.position.y;
            float bottom = top + md.height;
            float center = (top + bottom) * 0.5f;
            float d = Mathf.Abs(center - anchorWorldY);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        anchorIndex = best;
        return (best >= 0) ? (-itemMetadata[best].position.y + itemMetadata[best].height * 0.5f) : anchorWorldY;
    }

    private void RestoreAnchorY(float anchorItemCenterY)
    {
        if (viewport == null) return;
        float viewH = viewport.rect.height;
        float currentCenterY = content.anchoredPosition.y + viewH * 0.5f;
        float delta = anchorItemCenterY - currentCenterY;
        var pos = content.anchoredPosition;
        pos.y = Mathf.Clamp(pos.y + delta, 0f, Mathf.Max(0f, content.sizeDelta.y - viewH));
        content.anchoredPosition = pos;

        // 立刻刷新一次可见区，避免边界卡顿
        UpdateVisibleRange();
    }
    
    #endregion

    #region 脏标记更新
    /// <summary>
    /// 标记需要更新
    /// </summary>
    protected virtual void MarkDirty(DirtyFlags flags)
    {
        dirtyFlags |= flags;

        // 关键：所有更新都丢进队列，帧末统一处理
        if (!isBatchingFrame)
        {
            isBatchingFrame = true;
            // 使用 LateUpdate 比协程更可靠（不会被 Canvas 重建打断）
            batchedActions.Add(CommitBatchedUpdates);
        }
    }
    
    
    private void CommitBatchedUpdates()
    {
        isBatchingFrame = false;

        // 1. 先统一更新 content 尺寸
        if (dirtyFlags.HasFlag(DirtyFlags.Content))
        {
            UpdateContentSize();
        }
        // 2. 处理局部重排（高度变化、删除、插入）
        if (pendingRecalcFrom >= 0)
        {
            RecalculateFromIndex(pendingRecalcFrom);
            pendingRecalcFrom = -1;
            UpdateVisibleRange(); // 重排后必须刷新可见区
        }
        else if (dirtyFlags.HasFlag(DirtyFlags.Visibility))
        {
            // 3. 正常滚动只更新可见性
            UpdateVisibleRange();
        }

        dirtyFlags = DirtyFlags.None;
    }
    
    #endregion

    #region 计算辅助
    protected virtual float GetColumnWidth()
    {
        float totalSpacing = (Mathf.Max(1, columnCount) - 1) * spacingX + padding.x * 2;
        float w = viewport.rect.width - totalSpacing;
        return Mathf.Max(1f, w / Mathf.Max(1, columnCount));
    }

    protected virtual int GetShortestColumnIndex()
    {
        int idx = 0;
        float min = columnHeights[0];
        for (int i = 1; i < columnHeights.Count; i++)
        {
            if (columnHeights[i] < min)
            {
                min = columnHeights[i];
                idx = i;
            }
        }
        return idx;
    }
    #endregion

    #region 钩子和日志
    protected virtual void OnItemRecycled(GameObject go) { }

    protected void Log(string message)
    {
        if (enableDebugLog)
            Debug.Log($"[InfiniteScrollView] {message}");
    }

    protected void LogWarning(string message)
    {
        if (enableDebugLog)
            Debug.LogWarning($"[InfiniteScrollView] {message}");
    }
    #endregion

    private void LateUpdate()
    {
        if (batchedActions.Count == 0) return;
        for (int i = 0; i < batchedActions.Count; i++) batchedActions[i]?.Invoke();
        batchedActions.Clear();
    }
}

