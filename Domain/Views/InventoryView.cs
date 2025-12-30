
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class InventoryView : MonoBehaviour
{
    
    private UnityEngine.RectTransform InventoryContainerRect;
    private UnityEngine.RectTransform CharacterContainerRect;
    private UnityEngine.UI.ScrollRect InventoryScrollView;
    private UnityEngine.RectTransform ContentRect;
    private UnityEngine.RectTransform ItemDetailContainerRect;
    private TMPro.TextMeshProUGUI ItemNameText;
    private UnityEngine.UI.Image ItemImage;
    private TMPro.TextMeshProUGUI ItemCountText;
    private TMPro.TextMeshProUGUI EffectText;
    private TMPro.TextMeshProUGUI DescriptionText;
    private UnityEngine.RectTransform ButtonContainerRect;

    private void BindComponent()
    {
        var root = this.transform;

        InventoryContainerRect = root.Find("InventoryContainer") as RectTransform;

        CharacterContainerRect = root.Find("CharacterContainer") as RectTransform;

        InventoryScrollView = root.Find("InventoryContainer/InventoryScrollView")?.GetComponent<UnityEngine.UI.ScrollRect>();

        ContentRect = root.Find("InventoryContainer/InventoryScrollView/Viewport/Content") as RectTransform;

        ItemDetailContainerRect = root.Find("InventoryContainer/ItemDetailContainer") as RectTransform;

        ItemNameText = root.Find("InventoryContainer/ItemDetailContainer/ContentContainer/Top/ItemNameText")?.GetComponent<TMPro.TextMeshProUGUI>();

        ItemImage = root.Find("InventoryContainer/ItemDetailContainer/ContentContainer/Middle/ItemBK/ItemImage")?.GetComponent<UnityEngine.UI.Image>();

        ItemCountText = root.Find("InventoryContainer/ItemDetailContainer/ContentContainer/Middle/ItemCountText")?.GetComponent<TMPro.TextMeshProUGUI>();

        EffectText = root.Find("InventoryContainer/ItemDetailContainer/ContentContainer/Bottom/ItemInfoScrollView/Viewport/Content/EffectText")?.GetComponent<TMPro.TextMeshProUGUI>();

        DescriptionText = root.Find("InventoryContainer/ItemDetailContainer/ContentContainer/Bottom/ItemInfoScrollView/Viewport/Content/DescriptionText")?.GetComponent<TMPro.TextMeshProUGUI>();

        ButtonContainerRect = root.Find("InventoryContainer/ItemDetailContainer/ButtonContainer") as RectTransform;

    }



    [Header("背包格子设置")]
    public GameObject slotPrefab;
    public int columns = 6;
    public float slotSize = 80f;
    public float spacing = 5f;


    private readonly Dictionary<int, BackpackSlotUI> activeSlots = new Dictionary<int, BackpackSlotUI>();
    private readonly Queue<BackpackSlotUI> freeSlots = new();
    private readonly HashSet<int> pendingUpdateIndices = new HashSet<int>();
    private readonly List<int> updateBatchList = new List<int>();
    private bool updateScheduled = false;
    private Coroutine scrollCoroutine;
    private int lastFirstVisible = -1;
    private int lastLastVisible = -1;
    private int totalSlots = 0;

    
    [Header("Action Buttons Settings")] 
    public GameObject actionButtonPrefab;

    
    private float detailVisibleX;
    private float detailHiddenX;
    private bool isDetailOpen = false;
    private List<ItemDetailActionButton> actionButtons = new List<ItemDetailActionButton>();
    
    
    private NavigationController controller;

    private void Awake()
    {
        BindComponent();
        detailVisibleX = ItemDetailContainerRect.anchoredPosition.x;
        detailHiddenX = detailVisibleX + ItemDetailContainerRect.rect.width + 50f;

        Vector2 startPos = ItemDetailContainerRect.anchoredPosition;
        startPos.x = detailHiddenX;
        ItemDetailContainerRect.anchoredPosition = startPos;
        
        PoolService.Instance.Preload(slotPrefab, (columns + 10) * columns, (obj) =>
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(slotSize, slotSize);
        });
        

        InventoryScrollView.onValueChanged.AddListener(OnScroll);
    }

    public void Initialize(NavigationController controller)
    {
        this.controller = controller;
    }
    
    private void Update()
    {
        if (isDetailOpen && Input.GetMouseButtonDown(0))
        {
            bool isClickInside = RectTransformUtility.RectangleContainsScreenPoint(
                ItemDetailContainerRect, 
                Input.mousePosition, 
                null
            );

            if (!isClickInside)
            {
                HideItemDetail();
            }
        }
    }
    
    public void ResizeInventory(int newSize)
    {
        totalSlots = RoundUpToColumns(newSize);
        AdjustContentSize();
        RefreshAllVisibleSlots();
    }
    
    
    private void OnSlotClicked(BackpackSlotUI slotUI)
    {
        // 获取物品数据
        var key = slotUI.SlotKey;
        if (controller.GetSlotItem(key, out var item) && item != null)
        {
            ShowItemDetail(item);
        }
    }
    
    private void ShowItemDetail(ItemData item)
    {
        // 1. 填充数据
        if (ItemNameText) ItemNameText.text = item.ItemName ?? "Unknown Item"; // 假设 ItemData 有 Name 字段
        if (ItemCountText) ItemCountText.text = item.IsStack ? item.ItemCount.ToString() : "1";
        if (DescriptionText) DescriptionText.text = item.Description ?? ""; // 假设 ItemData 有 Description
        if (EffectText) EffectText.text = "Effect Info Here"; 
        
        if (ItemImage)
        {
            ItemImage.sprite = ResourceService.Instance.LoadResource<Sprite>($"Sprites/Items/{item.ItemType}/{item.ItemTemplateId}");
        }

        RefreshActionButtons(item);
        
        ItemDetailContainerRect.DOKill(); 
        ItemDetailContainerRect.DOAnchorPosX(detailVisibleX, 0.4f).SetEase(Ease.OutQuad);
        
        
        isDetailOpen = true;
    }

    private void RefreshActionButtons(ItemData item)
    {
        List<ItemActionInfo> actions = GetActionsForItem(item);
        if (actions.Count > actionButtons.Count)
        {
            for (int i = actionButtons.Count; i < actions.Count; i++)
            {
                var actionButton = PoolService.Instance.Spawn(actionButtonPrefab, ButtonContainerRect).GetComponent<ItemDetailActionButton>();
                actionButtons.Add(actionButton);
            }
        }else if (actions.Count < actionButtons.Count)
        {
            for (int i = actions.Count; i < actionButtons.Count; i++)
            {
                PoolService.Instance.Despawn(actionButtons[i].gameObject, false);
            }
        }


        for (int i = 0; i < actionButtons.Count; i++)
        {
            actionButtons[i].Initialize(
                actions[i].ButtonText, 
                actions[i].OnClick, 
                actions[i].IsDestructive ? Color.red : Color.white);
            
        }
    }

    private List<ItemActionInfo> GetActionsForItem(ItemData item)
    {
        var actions = new List<ItemActionInfo>();
        switch (item.ItemType)
        {
            case ItemType.Consumable:
                actions.Add(new ItemActionInfo("使用", () => OnUseItem(item)));
                if (item.ItemCount > 1)
                {
                    actions.Add(new ItemActionInfo("全部使用", () => OnUseAllItem(item)));
                }
                actions.Add(new ItemActionInfo("丢弃", () => OnDropItem(item), true));
                break;
        }
        
        return actions;
    }
    
    private void OnUseItem(ItemData item){}
    private void OnUseAllItem(ItemData item){}
    private void OnDropItem(ItemData item){}
    
    private void HideItemDetail()
    {
        if (!isDetailOpen) return;

        if (ItemDetailContainerRect != null)
        {
            ItemDetailContainerRect.DOKill();
            ItemDetailContainerRect.DOAnchorPosX(detailHiddenX, 0.3f).SetEase(Ease.InQuad);
        }
        
        isDetailOpen = false;
    }

    public void OpenInventory(int maxSize)
    {
        if (maxSize > 0)
        {
            totalSlots = RoundUpToColumns(maxSize);
            AdjustContentSize();
            for (int i = 0; i < (columns + 10) * columns; i++)
            {
                var go = PoolService.Instance.Spawn(slotPrefab, ContentRect.position, Quaternion.identity);
                go.transform.SetParent(ContentRect, false);
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

    public void UpdateSlotsContent(IReadOnlyList<SlotKey> slots)
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
                slotGo.UpdateSlot(key, controller.GetSlotItem(key, out var item) ? item : null);
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

        ContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        ContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
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
        float scrollY = ContentRect.anchoredPosition.y;
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
        
        var toRemove = new List<int>();
        foreach (var kv in activeSlots)
        {
            int index = kv.Key;
            if (index < currentFirstVisible || index > currentLastVisible)
            {
                var slotUI = kv.Value;
                slotUI.gameObject.SetActive(false);
                slotUI.OnClick -= OnSlotClicked;
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
                slot.OnClick += OnSlotClicked;
                continue;
            }

            if (freeSlots.Count == 0)
            {
                var extraGo = PoolService.Instance.Spawn(slotPrefab, ContentRect.position, Quaternion.identity);
                extraGo.transform.SetParent(ContentRect, false);
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
            slotUI.OnClick += OnSlotClicked;
            activeSlots.Add(i, slotUI);
            
            var key = new SlotKey { Container = SlotContainerType.Inventory, Index = i };
            slotUI.UpdateSlot(key, controller.GetSlotItem(key, out var item) ? item : null);
        }
    }
    
    
    private void RefreshAllVisibleSlots()
    {
        foreach (var kvp in activeSlots)
        {
            UpdateSlotContent(new SlotKey { Container = SlotContainerType.Inventory, Index = kvp.Key });
        }
    }
    
    
    
}
