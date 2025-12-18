using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BackpackSlotUI : MonoBehaviour, IPooledObject, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public SlotKey SlotKey;
    public bool HasItem;
    public Sprite DefaultSprite;
    public float tooltipPadding = 12f;
    public RectTransform Rect;

    private Image iconImage;
    private TMP_Text countText;

    private static Image DragIcon;
    private static RectTransform DragIconRect;
    private bool dragging;
    private bool hasEntered;

    private void Awake()
    {
        Rect = GetComponent<RectTransform>();
        countText = transform.Find("Count").GetComponent<TMP_Text>();
        iconImage = transform.Find("Icon").GetComponent<Image>();
    }

    public void UpdateSlot(SlotKey key, ItemData item)
    {
        SlotKey = key;

        if (item == null)
        {
            iconImage.sprite = DefaultSprite;
            countText.text = "";
            HasItem = false;
            return;
        }

        HasItem = true;
        iconImage.sprite = ResourceService.Instance.LoadResource<Sprite>($"Sprites/Items/{item.ItemType}/{item.ItemTemplateId}");
        countText.text = item.IsStack ? item.ItemCount.ToString() : "";
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if(!HasItem) return;
        dragging = true;
        EnsureDragIcon();
        iconImage.sprite = DefaultSprite;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(!dragging) return;
        PositionDragIcon(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(!dragging) return;
        dragging = false;

        HideDragIcon();

        var target = GetSlotUnderPointer<BackpackSlotUI>(eventData);
        if (target != null && target != this)
        {
            var view = UIService.Instance.GetView<CharacterInfoView>();
            view.RequestSwap(
                SlotKey,
                target.SlotKey
            );
        }
        else
        {
            iconImage.sprite = DragIcon.sprite;
        }

    }

    private T GetSlotUnderPointer<T>(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var result in results)
        {
            var slot = result.gameObject.GetComponent<T>();
            if (slot != null) return slot;
        }
        return default;
    }

    private void EnsureDragIcon()
    {
        if (DragIcon == null)
        {
            var go = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(UIService.Instance.GetView<CharacterInfoView>().transform, false);
            DragIcon = go.GetComponent<Image>();
            DragIcon.raycastTarget = false;
            DragIconRect = go.GetComponent<RectTransform>();
            DragIconRect.sizeDelta = iconImage.rectTransform.sizeDelta;
        }
        DragIcon.sprite = iconImage.sprite;
        DragIcon.gameObject.SetActive(true);
    }

    private void PositionDragIcon(PointerEventData eventData)
    {
        if (DragIconRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            UIService.Instance.ScreenCanvas.transform as RectTransform, eventData.position, Camera.main, out var localPos);
        DragIconRect.anchoredPosition = localPos;
    }

    private void HideDragIcon() => DragIcon?.gameObject.SetActive(false);

    public void OnPointerEnter(PointerEventData _) 
    {
        if (HasItem && !hasEntered)
        {
            hasEntered = true;
            UIService.Instance.GetView<CharacterInfoView>().ShowItemInfo(transform as RectTransform, SlotKey, tooltipPadding);
        }
    }

    public void OnPointerExit(PointerEventData _) 
    {
        hasEntered = false;
        UIService.Instance.GetView<CharacterInfoView>().HideItemInfo();
    }

    public void OnObjectSpawn()
    {
        Rect.localPosition = Vector3.zero;
    }

    public void OnObjectDespawn()
    {
   
    }
}
