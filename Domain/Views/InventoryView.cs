using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private UnityEngine.RectTransform EquipContainerRect;
    private UnityEngine.RectTransform QuickBarContainerRect;

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

        EquipContainerRect = root.Find("CharacterContainer/EquipContainer") as RectTransform;

        QuickBarContainerRect = root.Find("CharacterContainer/QuickBarContainer") as RectTransform;

    }


    [Header("Slot Settings")]
    public GameObject slotPrefab;
    public int columns = 6;
    public float slotSize = 80f;
    public float spacing = 5f;

    [Header("Action Buttons Settings")] 
    public GameObject actionButtonPrefab;

    public Image DragIcon;
    public RectTransform DragIconRect;
    
    private NavigationController controller;
    
    private List<SlotKey> currentDisplayList = new List<SlotKey>();
    
    private readonly Dictionary<int, BackpackSlotUI> activeSlots = new Dictionary<int, BackpackSlotUI>();
    private readonly Queue<BackpackSlotUI> freeSlots = new Queue<BackpackSlotUI>();
    private readonly HashSet<SlotKey> pendingUpdateKeys = new HashSet<SlotKey>();
    private bool updateScheduled = false;
    
    private readonly BackpackSlotUI[] equipSlots = new BackpackSlotUI[6];
    private readonly BackpackSlotUI[] quickSlots = new BackpackSlotUI[3];
    
    private int currentFirstVisible = -1;
    private int currentLastVisible = -1;
    private int totalDisplaySlots = 0;
    
    private float detailVisibleX;
    private float detailHiddenX;
    private bool isDetailOpen = false;
    private List<ItemDetailActionButton> actionButtons = new List<ItemDetailActionButton>();

    private void Awake()
    {
        BindComponent();
        InitializeDetailPanel();
        InitializePool();
        InitializeFixedSlots();
        
        InventoryScrollView.onValueChanged.AddListener(OnScroll);
    }

    private void Update()
    {
        if (isDetailOpen && Input.GetMouseButtonDown(0))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(ItemDetailContainerRect, Input.mousePosition, null))
            {
                HideItemDetail();
            }
        }
    }

    public void Initialize(NavigationController controller)
    {
        this.controller = controller;
    }
    
    public void UpdateDisplayList(List<SlotKey> newDisplayList)
    {
        currentDisplayList = newDisplayList ?? new List<SlotKey>();
        totalDisplaySlots = currentDisplayList.Count;

        // 重新计算内容高度
        AdjustContentSize();
        pendingUpdateKeys.Clear(); 
        Canvas.ForceUpdateCanvases();
        RefreshAllVisibleSlots(forceRebuild: true);
    }
    
    public void UpdateSlotContent(SlotKey key)
    {
        if (key.Container == SlotContainerType.Equipment)
        {
            UpdateFixedSlot(equipSlots, key);
            return;
        }
        
        if (key.Container == SlotContainerType.QuickBar)
        {
            UpdateFixedSlot(quickSlots, key);
            return;
        }
        
        if (key.Container == SlotContainerType.Inventory)
        {
            if (pendingUpdateKeys.Add(key))
            {
                ScheduleBatchUpdate();
            }
        }
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

        if (pendingUpdateKeys.Count == 0) return;
        
        foreach (var kvp in activeSlots)
        {
            int uiIndex = kvp.Key;
            
            if (uiIndex >= currentDisplayList.Count) continue;

            SlotKey displayingKey = currentDisplayList[uiIndex];
            if (pendingUpdateKeys.Contains(displayingKey))
            {
                RefreshSingleSlotUI(kvp.Value, displayingKey);
            }
        }

        pendingUpdateKeys.Clear();
    }
    
    private void RefreshSingleSlotUI(BackpackSlotUI slotUI, SlotKey key)
    {
        if (controller.GetSlotItem(key, out var item))
        {
            slotUI.UpdateSlot(key, item);
        }
        else
        {
            slotUI.UpdateSlot(key, null);
        }
    }
    
    public void ResetScrollPosition()
    {
        if (InventoryScrollView != null)
        {
            InventoryScrollView.verticalNormalizedPosition = 1f;
        }
    }
    
    public void OnFilterChanged(string name, QualityType quality, ItemType type)
    {
        controller?.ApplyFilter(name, quality, type);
    }
    

    private void OnScroll(Vector2 scrollPos)
    {
        if (totalDisplaySlots == 0) return;
        
        if (CalculateVisibleRange()) 
        {
            UpdateVisibleSlots();
        }
    }

    private bool CalculateVisibleRange()
    {
        float scrollY = ContentRect.anchoredPosition.y;
        float rowHeight = slotSize + spacing;

        int firstVisibleRow = Mathf.Max(0, Mathf.FloorToInt(scrollY / rowHeight));
        float viewportHeight = InventoryScrollView.viewport.rect.height;
        int visibleRowCount = Mathf.CeilToInt(viewportHeight / rowHeight);
        
        int bufferRows = 2;
        
        int newFirstVisible = Mathf.Max(0, (firstVisibleRow - bufferRows) * columns);
        int newLastVisible = Mathf.Min(
            totalDisplaySlots - 1,
            (firstVisibleRow + visibleRowCount + bufferRows) * columns + (columns - 1));

        if (newFirstVisible == currentFirstVisible && newLastVisible == currentLastVisible)
            return false;

        currentFirstVisible = newFirstVisible;
        currentLastVisible = newLastVisible;
        return true;
    }

    private void UpdateVisibleSlots()
    {
        var toRemove = new List<int>();
        foreach (var kv in activeSlots)
        {
            int index = kv.Key;
            if (index < currentFirstVisible || index > currentLastVisible || index >= totalDisplaySlots)
            {
                RecycleSlot(kv.Value);
                toRemove.Add(index);
            }
        }
        foreach (int index in toRemove) activeSlots.Remove(index);
        
        for (int i = currentFirstVisible; i <= currentLastVisible; i++)
        {
            if (activeSlots.ContainsKey(i)) continue;
            var slotUI = SpawnSlot();
            int row = i / columns;
            int column = i % columns;
            float x = column * (slotSize + spacing);
            float y = -row * (slotSize + spacing);
            slotUI.Rect.anchoredPosition = new Vector2(x, y);
            
            SlotKey realKey = currentDisplayList[i];
            
            controller.GetSlotItem(realKey, out var item);
            slotUI.UpdateSlot(realKey, item);

            activeSlots.Add(i, slotUI);
        }
    }

    private void RefreshAllVisibleSlots(bool forceRebuild = false)
    {
        if (forceRebuild)
        {
            foreach (var slot in activeSlots.Values) RecycleSlot(slot);
            activeSlots.Clear();
            currentFirstVisible = -1;
            OnScroll(Vector2.zero);
        }
        else
        {
            foreach (var kvp in activeSlots)
            {
                int i = kvp.Key;
                if (i < currentDisplayList.Count)
                {
                    SlotKey realKey = currentDisplayList[i];
                    controller.GetSlotItem(realKey, out var item);
                    kvp.Value.UpdateSlot(realKey, item);
                }
            }
        }
    }

    private void AdjustContentSize()
    {
        int rows = Mathf.CeilToInt((float)totalDisplaySlots / columns);
        float height = rows * (slotSize + spacing) - spacing;
        float width = columns * (slotSize + spacing) - spacing;

        ContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(height, 0));
        ContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }
    

    private void InitializePool()
    {
        PoolService.Instance.Preload(slotPrefab, (columns + 5) * 6, (obj) =>
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(slotSize, slotSize);
        });
    }

    private BackpackSlotUI SpawnSlot()
    {
        BackpackSlotUI slotUI;
        if (freeSlots.Count > 0)
        {
            slotUI = freeSlots.Dequeue();
        }
        else
        {
            var go = PoolService.Instance.Spawn(slotPrefab, ContentRect.position, Quaternion.identity);
            go.transform.SetParent(ContentRect, false);
            slotUI = go.GetComponent<BackpackSlotUI>();
        }
        
        slotUI.gameObject.SetActive(true);
        slotUI.OnClick += OnSlotClicked;
        slotUI.OnDragStarted += OnSlotBeginDrag;
        slotUI.OnDragUpdated += OnSlotDrag;
        slotUI.OnDragEnded   += OnSlotEndDrag;
        return slotUI;
    }

    private void RecycleSlot(BackpackSlotUI slotUI)
    {
        slotUI.OnClick -= OnSlotClicked;
        slotUI.OnDragStarted -= OnSlotBeginDrag;
        slotUI.OnDragUpdated -= OnSlotDrag;
        slotUI.OnDragEnded   -= OnSlotEndDrag;
        slotUI.gameObject.SetActive(false);
        freeSlots.Enqueue(slotUI);
    }
    
    private void OnSlotDrag(BackpackSlotUI slot, PointerEventData eventData)
    {
        UpdateDragIconPosition(eventData);
    }

    private void OnSlotEndDrag(BackpackSlotUI slot, PointerEventData eventData)
    {
        DragIcon.gameObject.SetActive(false);
    }

    private void OnSlotBeginDrag(BackpackSlotUI slotUI, PointerEventData eventData)
    {
        DragIcon.sprite = slotUI.GetCurrentIcon();
        DragIcon.gameObject.SetActive(true);
        UpdateDragIconPosition(eventData);
    }
    
    
    private void UpdateDragIconPosition(PointerEventData eventData)
    {
        if (DragIconRect == null) return;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)DragIconRect.parent, 
            eventData.position, 
            null,
            out var localPos
        );
        
        DragIconRect.anchoredPosition = localPos;
    }
    
    private void InitializeDetailPanel()
    {
        if (ItemDetailContainerRect == null) return;
        
        detailVisibleX = ItemDetailContainerRect.anchoredPosition.x;
        detailHiddenX = detailVisibleX + ItemDetailContainerRect.rect.width + 50f;

        var pos = ItemDetailContainerRect.anchoredPosition;
        pos.x = detailHiddenX;
        ItemDetailContainerRect.anchoredPosition = pos;
    }

    private void OnSlotClicked(BackpackSlotUI slotUI)
    {
        if (controller.GetSlotItem(slotUI.SlotKey, out var item) && item != null)
        {
            ShowItemDetail(slotUI.SlotKey, item);
        }
        else
        {
            HideItemDetail();
        }
    }

    private void ShowItemDetail(SlotKey slot, ItemData item)
    {
        if (ItemNameText) ItemNameText.text = item.ItemName ?? "Unknown";
        if (ItemCountText) ItemCountText.text = $"品质: {item.ItemCount}";
        if (DescriptionText) DescriptionText.text = item.Description ?? "";
        
        if (EffectText)
        {
            EffectText.text = "";
            if (item is EquipData equip && equip.BaseAttributes != null)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var attr in equip.BaseAttributes)
                {
                    sb.AppendLine($"+{attr.Key}: {attr.Value}");
                }
                EffectText.text = sb.ToString();
            }
        }

        // 填充图标
        if (ItemImage)
        {
            // 注意：这里需要根据你的资源加载逻辑修改
            // ItemImage.sprite = ...
        }

        RefreshActionButtons(slot, item);
        
        ItemDetailContainerRect.DOKill();
        ItemDetailContainerRect.DOAnchorPosX(detailVisibleX, 0.3f).SetEase(Ease.OutQuad);
        isDetailOpen = true;
    }

    private void HideItemDetail()
    {
        if (!isDetailOpen) return;
        ItemDetailContainerRect.DOKill();
        ItemDetailContainerRect.DOAnchorPosX(detailHiddenX, 0.2f).SetEase(Ease.InQuad);
        isDetailOpen = false;
    }

    private void RefreshActionButtons(SlotKey slot, ItemData item)
    {
        var actions = controller.GetActionsForItem(slot, item);
        
        while (actionButtons.Count < actions.Count)
        {
            var btn = PoolService.Instance.Spawn(actionButtonPrefab, ButtonContainerRect).GetComponent<ItemDetailActionButton>();
            actionButtons.Add(btn);
        }
        
        for (int i = actions.Count; i < actionButtons.Count; i++)
        {
            actionButtons[i].gameObject.SetActive(false);
        }
        
        for (int i = 0; i < actions.Count; i++)
        {
            var btn = actionButtons[i];
            btn.gameObject.SetActive(true);
            var info = actions[i];
            btn.Initialize(info.ButtonText, info.OnClick, info.IsDestructive ? Color.red : Color.white);
        }
    }
    
    

    private void InitializeFixedSlots()
    {
        if (EquipContainerRect != null)
        {
            for (int i = 0; i < EquipContainerRect.childCount; i++)
            {
                if (i >= equipSlots.Length) break;
                var slot = EquipContainerRect.GetChild(i).GetComponent<BackpackSlotUI>();
                if (slot != null)
                {
                    slot.SlotKey = new SlotKey(SlotContainerType.Equipment, i);
                    slot.OnClick += OnSlotClicked;
                    equipSlots[i] = slot;
                }
            }
        }
        
        if (QuickBarContainerRect != null)
        {
            for (int i = 0; i < QuickBarContainerRect.childCount; i++)
            {
                if (i >= quickSlots.Length) break;
                var slot = QuickBarContainerRect.GetChild(i).GetComponent<BackpackSlotUI>();
                if (slot != null)
                {
                    slot.SlotKey = new SlotKey(SlotContainerType.QuickBar, i);
                    slot.OnClick += OnSlotClicked;
                    quickSlots[i] = slot;
                }
            }
        }
    }

    private void UpdateFixedSlot(BackpackSlotUI[] slotsArray, SlotKey key)
    {
        if (key.Index < 0 || key.Index >= slotsArray.Length) return;
        
        var slotUI = slotsArray[key.Index];
        if (slotUI == null) return;
        
        controller.GetSlotItem(key, out var item);
        slotUI.UpdateSlot(key, item);
    }
    
}