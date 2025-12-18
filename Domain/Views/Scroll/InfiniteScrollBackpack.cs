using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfiniteScrollBackpack : MonoBehaviour
{
    public GameObject slotPrefab;
    public int columns = 6;
    public float slotSize = 80f;
    public float spacing = 5f;
    
    private RectTransform contentRect;
    private ScrollRect scrollRect;

    private readonly Dictionary<int, BackpackSlotUI> activeSlots = new Dictionary<int, BackpackSlotUI>();
    private readonly Queue<BackpackSlotUI> freeSlots = new();
    private readonly HashSet<int> pendingUpdateIndices = new HashSet<int>();
    private readonly List<int> updateBatchList = new List<int>();
    private bool updateScheduled = false;
    private Coroutine scrollCoroutine;
    private int lastFirstVisible = -1;
    private int lastLastVisible = -1;
    private int totalSlots = 0;
    
    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        contentRect = scrollRect.content;
        PoolService.Instance.Preload(slotPrefab, (columns + 10) * columns);
        scrollRect.onValueChanged.AddListener(OnScroll);
    }
    
    public void ResizeInventory(int newSize)
    {
        totalSlots = RoundUpToColumns(newSize);
        AdjustContentSize();
        RefreshAllVisibleSlots();
    }

    public void Initialize(int maxSize)
    {
        if (maxSize > 0)
        {
            totalSlots = RoundUpToColumns(maxSize);
            AdjustContentSize();
            for (int i = 0; i < (columns + 10) * columns; i++)
            {
                var go = PoolService.Instance.Spawn(slotPrefab, contentRect.position, Quaternion.identity);
                go.transform.SetParent(contentRect, false);
                var ui = go.GetComponent<BackpackSlotUI>();
                go.SetActive(false);
                freeSlots.Enqueue(ui);
            }
            OnScroll(Vector2.zero);
        }
    }
    
    public void UpdateSlotContent(SlotKey slot)
    {
        if (slot.Container != SlotContainerType.Inventory) return;

        if (activeSlots.ContainsKey(slot.Index))
        {
            if (pendingUpdateIndices.Add(slot.Index))
            {
                ScheduleBatchUpdate();
            }
        }
    }

    public void UpdateBatchSlots(IReadOnlyList<SlotKey> slots)
    {
        foreach (var slot in slots)
        {
            if (slot.Container == SlotContainerType.Inventory)
            {
                pendingUpdateIndices.Add(slot.Index);
            }
        }
        ScheduleBatchUpdate();
    }
    
    private void ScheduleBatchUpdate()
    {
        if (!updateScheduled)
        {
            updateScheduled = true;
            Canvas.willRenderCanvases += ProcessPendingUpdates;
        }
    }
    
    private void ProcessPendingUpdates()
    {
        Canvas.willRenderCanvases -= ProcessPendingUpdates;
        updateScheduled = false;

        updateBatchList.Clear();
        updateBatchList.AddRange(pendingUpdateIndices);
        pendingUpdateIndices.Clear();
        
        foreach (int index in updateBatchList)
        {
            if (activeSlots.TryGetValue(index, out var slotGo))
            {
                var key = new SlotKey { Container = SlotContainerType.Inventory, Index = index };

                slotGo.UpdateSlot(key, UIService.Instance.GetView<CharacterInfoView>().GetSlotItem(key, out var item) ? item : null);
            }
        }
    }
    
    private int RoundUpToColumns(int count)
    {
        if (columns <= 0) return count;  
        return ((count + columns - 1) / columns) * columns;
    }

    private void AdjustContentSize()
    {
        int rows = Mathf.CeilToInt((float)totalSlots / columns);
        float contentHeight = rows * (slotSize + spacing) - spacing;
        float contentWidth = columns * (slotSize + spacing) - spacing;

        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
    }

    private void OnScroll(Vector2 scrollPos)
    {
        if (!CalculateVisibleRange()) 
            return;
        UpdateVisibleSlots();
    }
    
    private int currentFirstVisible;
    private int currentLastVisible;
    
    private bool CalculateVisibleRange()
    {
        float scrollY = contentRect.anchoredPosition.y;
        float rowHeight = slotSize + spacing;

        int firstVisibleRow = Mathf.FloorToInt(scrollY / rowHeight);

        float viewportHeight = GetComponent<RectTransform>().rect.height;
        int visibleRowCount = Mathf.CeilToInt(viewportHeight / rowHeight);

        int bufferRows = 2;
        int newFirstVisible = Mathf.Max(0, (firstVisibleRow - bufferRows) * columns);
        int newLastVisible = Mathf.Min(
            totalSlots - 1,
            (firstVisibleRow + visibleRowCount + bufferRows) * columns + (columns - 1));

        // ✅ 如果范围没变，直接不更新
        if (newFirstVisible == lastFirstVisible && newLastVisible == lastLastVisible)
            return false;

        currentFirstVisible = lastFirstVisible = newFirstVisible;
        currentLastVisible  = lastLastVisible  = newLastVisible;
        return true;
    }

    private void UpdateVisibleSlots()
    {
        if (totalSlots == 0) return;
    
        // 移除不可见的槽位（优化版）
        var toRemove = new List<int>();
        foreach (var kv in activeSlots)
        {
            int index = kv.Key;
            if (index < currentFirstVisible || index > currentLastVisible)
            {
                var slotUI = kv.Value;
                slotUI.gameObject.SetActive(false);
                freeSlots.Enqueue(slotUI);
                toRemove.Add(index);
            }
        }

        foreach (int index in toRemove)
        {
            activeSlots.Remove(index);
        }
        for (int i = currentFirstVisible; i <= currentLastVisible; i++)
        {
            if (activeSlots.TryGetValue(i, out var slot))
            {
                slot.gameObject.SetActive(true);
                continue;
            }

            if (freeSlots.Count == 0)
            {
                var extraGo = PoolService.Instance.Spawn(slotPrefab, contentRect.position, Quaternion.identity);
                extraGo.transform.SetParent(contentRect, false);
                freeSlots.Enqueue(extraGo.GetComponent<BackpackSlotUI>());
            }

            var slotUI = freeSlots.Dequeue();
            var rt = slotUI.Rect;

            int row = i / columns;
            int column = i % columns;
            float xPosition = column * (slotSize + spacing);
            float yPosition = -row * (slotSize + spacing);
            rt.anchoredPosition = new Vector2(xPosition, yPosition);

            slotUI.gameObject.SetActive(true);

            activeSlots.Add(i, slotUI);
            
            var key = new SlotKey { Container = SlotContainerType.Inventory, Index = i };
            slotUI.UpdateSlot(key, UIService.Instance.GetView<CharacterInfoView>().GetSlotItem(key, out var item) ? item : null);
        }
    }
    
    
    private void RefreshAllVisibleSlots()
    {
        foreach (var kvp in activeSlots)
        {
            UpdateSlotContent(new SlotKey { Container = SlotContainerType.Inventory, Index = kvp.Key });
        }
    }
    
    private void CreateSlot(int index)
    {
        GameObject slot = PoolService.Instance.Spawn(slotPrefab, contentRect.transform.position, Quaternion.identity);
        slot.transform.SetParent(contentRect, false);
        slot.SetActive(true);

        int row = index / columns;
        int column = index % columns;
        float xPosition = column * (slotSize + spacing);
        float yPosition = -row * (slotSize + spacing);
        slot.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPosition, yPosition);
        
        activeSlots.Add(index, slot.GetComponent<BackpackSlotUI>());
        UpdateSlotContent(new SlotKey { Container = SlotContainerType.Inventory, Index = index });
    }
}