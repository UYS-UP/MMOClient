using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterInfoView : BaseView
{
    private CharacterInfoController controller;
    
    private TMPro.TextMeshProUGUI GoldText;
    private UnityEngine.UI.Button SortButton;
    private InfiniteScrollBackpack Backpack;
    private UnityEngine.UI.Button CloseButton;
    private UnityEngine.RectTransform ItemInfoRect;
    
    private TextMeshProUGUI[] ItemInfoTexts;

    private void BindComponent()
    {
        var root = this.transform;

        {
            var t = root.Find("InventoryFrame/Content/GoldText");
            GoldText = t ? t.GetComponent<TMPro.TextMeshProUGUI>() : null;
        }

        {
            var t = root.Find("InventoryFrame/Content/SortButton");
            SortButton = t ? t.GetComponent<UnityEngine.UI.Button>() : null;
        }

        {
            var t = root.Find("InventoryFrame/Content/SlotScrollView");
            Backpack = t ? t.GetComponent<InfiniteScrollBackpack>() : null;
        }

        {
            var t = root.Find("CharacterInfoFrame/Header/CloseButton");
            CloseButton = t ? t.GetComponent<UnityEngine.UI.Button>() : null;
        }

        {
            var t = root.Find("ItemInfo");
            ItemInfoRect = t ? t.GetComponent<UnityEngine.RectTransform>() : null;
        }

    }
    
    protected override void Awake()
    {
        base.Awake();
        BindComponent();
        controller = new CharacterInfoController(this);
        
        ItemInfoTexts = new TextMeshProUGUI[ItemInfoRect.childCount];
        for (var i = 0; i < ItemInfoRect.childCount; i++)
        {
            ItemInfoTexts[i] = ItemInfoRect.GetChild(i).GetComponent<TextMeshProUGUI>();
        }
        CloseButton.onClick.AddListener(() =>
        {
            UIService.Instance.HidePanel<CharacterInfoView>();
            InputBindService.Instance.UIIsOpen = false;
        });

    }

    public void Initialize(int maxSize)
    {
        Backpack.Initialize(maxSize);
    }
    
    private void OnSortButtonClick()
    {
        // 排序
    }
    
    private void OnDestroy()
    {
        CloseButton.onClick.RemoveAllListeners();
        controller?.Dispose();
    }

    public void UpdateBatchSlots(IReadOnlyList<SlotKey> slots)
    {
        Backpack.UpdateBatchSlots(slots);
    }

    public void UpdateSlotContent(SlotKey slot)
    {
        Backpack.UpdateSlotContent(slot);
    }

    public void ResizeInventory(int newSize)
    {
        Backpack.ResizeInventory(newSize);
    }
    
    public bool GetSlotItem(SlotKey slot, out ItemData value)
    {
        return controller.GetSlotItem(slot, out value);
    }
    

    public void ShowItemInfo(RectTransform slotRT, SlotKey slot, float padding = 12f)
    {
        if (ItemInfoRect == null || !controller.GetSlotItem(slot, out var item)) return;
    
        SetItemInfoContent(item);
    
        ItemInfoRect.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(ItemInfoRect);
    
        PositionTooltipAutoPivot(slotRT, padding);
        ClampTooltipInsideCanvas();
    }
    
    public void HideItemInfo()
    {
        if (ItemInfoRect != null)
        {
            ItemInfoRect.gameObject.SetActive(false);
        }
    }
    
    
    private void SetItemInfoContent(ItemData data)
    {
        // if (itemInfoTexts.TryGetValue(-2, out var name))
        // {
        //     name.text = $"{data.ItemName}({data.QuantityType})";
        // }
        //
        // if (itemInfoTexts.TryGetValue(-1, out var level))
        // {
        //     level.text = data.ItemType == ItemType.Equip ? ((EquipData)data).Level.ToString() : "";
        // }
        //
        // for (int i = 0; i < 8; i++)
        // {
        //     if (itemInfoTexts.TryGetValue(i, out var prop))
        //     {
        //         prop.text = "";
        //     }
        // }
    }
    
    private void PositionTooltipAutoPivot(RectTransform slotRT, float padding)
    {
        var canvas = UIService.Instance.ScreenCanvas;
        var cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;
    
        Vector2 slotScreen = RectTransformUtility.WorldToScreenPoint(cam, slotRT.TransformPoint(slotRT.rect.center));
    
        Rect pxRect = canvas.pixelRect;
        Vector2 screenCenter = pxRect.center;
        bool onRightHalf = slotScreen.x >= screenCenter.x;
        bool onUpperHalf = slotScreen.y >= screenCenter.y;
    
        ItemInfoRect.pivot = new Vector2(onRightHalf ? 1f : 0f, onUpperHalf ? 1f : 0f);
    
        Vector2 screenOffset = new Vector2(onRightHalf ? -padding : padding,
                                           onUpperHalf ? -padding : padding);
        Vector2 targetScreen = slotScreen + screenOffset;
    
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)ItemInfoRect.parent, targetScreen, cam, out var targetLocal);
        ItemInfoRect.anchoredPosition = targetLocal;
    }
    
    private void ClampTooltipInsideCanvas()
    {
        var canvas = UIService.Instance.ScreenCanvas;
        var cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;
    
        Vector3[] corners = new Vector3[4];
        ItemInfoRect.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
    
        Rect bounds = canvas.pixelRect;
        Vector2 offset = Vector2.zero;
        if (max.x > bounds.xMax) offset.x += bounds.xMax - max.x;
        if (min.x < bounds.xMin) offset.x += bounds.xMin - min.x;
        if (max.y > bounds.yMax) offset.y += bounds.yMax - max.y;
        if (min.y < bounds.yMin) offset.y += bounds.yMin - min.y;
    
        if (offset != Vector2.zero)
        {
            Vector2 currentScreen = RectTransformUtility.WorldToScreenPoint(cam, ItemInfoRect.position);
            Vector2 targetScreen = currentScreen + offset;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)ItemInfoRect.parent, targetScreen, cam, out var targetLocal);
            ItemInfoRect.anchoredPosition = targetLocal;
        }
    }

    public void RequestSwap(SlotKey slotKey, SlotKey targetSlotKey)
    {
        controller.RequestSwap(slotKey, targetSlotKey);
    }
}