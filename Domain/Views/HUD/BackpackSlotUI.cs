using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public struct ItemActionInfo
{
    public string ButtonText;
    public Action OnClick;
    public bool IsDestructive;

    public ItemActionInfo(string buttonText, Action onClick, bool isDestructive = false)
    {
        ButtonText = buttonText;
        OnClick = onClick;
        IsDestructive = isDestructive;
    }
}

public class BackpackSlotUI : MonoBehaviour, IPooledObject, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public SlotKey SlotKey;
    public bool HasItem;
    public Sprite DefaultSprite;
    public float tooltipPadding = 12f;
    public RectTransform Rect;
    private Image iconImage;
    private TMP_Text countText;
    private RectTransform DragIconRect;
    private bool dragging;
    private bool hasEntered;

    public event Action<BackpackSlotUI> OnClick;
    public Action<BackpackSlotUI, PointerEventData> OnDragStarted;
    public Action<BackpackSlotUI, PointerEventData> OnDragUpdated;
    public Action<BackpackSlotUI, PointerEventData> OnDragEnded;

    private void Awake()
    {
        Rect = GetComponent<RectTransform>();
        countText = transform.Find("Count").GetComponent<TMP_Text>();
        iconImage = transform.Find("Icon").GetComponent<Image>();
    }
    
    public Sprite GetCurrentIcon() => iconImage.sprite;

    public void UpdateSlot(SlotKey key, ItemData item)
    {
        SlotKey = key;
        if (item == null)
        {
            iconImage.color = Color.clear;
            countText.text = "";
            HasItem = false;
            return;
        }

        HasItem = true;
        iconImage.color = Color.white;
        iconImage.sprite = ResourceService.Instance.LoadResource<Sprite>($"Sprites/Items/{item.ItemType}/{item.TemplateId}");
        countText.text = item.IsStack ? item.ItemCount.ToString() : "";
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem) return;
        if (SlotKey.Container != SlotContainerType.Inventory) 
            return;
        dragging = true;
        var color = iconImage.color;
        color.a = 0.5f;
        iconImage.color = color;
        OnDragStarted?.Invoke(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(!dragging) return;
        OnDragUpdated?.Invoke(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(!dragging) return;
        dragging = false;
        OnDragEnded?.Invoke(this, eventData);
        var target = GetSlotUnderPointer<BackpackSlotUI>(eventData);
        if (target != null && target != this &&  target.SlotKey.Container == SlotContainerType.Inventory)
        {
            var view = UIService.Instance.GetView<NavigationView>();
            view.RequestSwap(
                SlotKey,
                target.SlotKey
            );
        }
        else
        {
            iconImage.color = Color.white;
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
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        Rect.DOScale(1.1f, 0.2f).SetEase(Ease.OutQuad);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        Rect.DOScale(1f, 0.2f).SetEase(Ease.InQuad);
    }
    

    public void OnObjectSpawn()
    {

    }

    public void OnObjectDespawn()
    {
        Rect.localPosition = Vector3.zero;
        OnClick = null; 
        OnDragStarted = null;
        OnDragUpdated = null;
        OnDragEnded = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!dragging && HasItem)
        {
            OnClick?.Invoke(this);
        }
    }



}
