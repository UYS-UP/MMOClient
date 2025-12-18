// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Cysharp.Threading.Tasks;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
//
// public class RoleInfoView : BaseView
// {
//     private InfiniteScrollBackpack backpack;
//     private const int batchSize = 300;
//     private bool requesting = false;
//     
//     private int currentPage;
//     private int maxPage;
//     
//     private RectTransform itemInfoRect;   // 拖引用，或在 Awake 里 Find
//     private Dictionary<int, TMP_Text> itemInfoTexts;       // 复用你原先的键位：-2=Name, -1=Level, 0..7=Property_i
//
//
//     protected override void Awake()
//     {
//         base.Awake();
//         backpack = transform.Find("Bag/ScrollView").GetComponent<InfiniteScrollBackpack>();
//         itemInfoRect = transform.Find("ItemInfo").GetComponent<RectTransform>();
//         itemInfoTexts = new Dictionary<int, TMP_Text>();
//         var nameText  = itemInfoRect.transform.Find("Name")?.GetComponent<TMP_Text>();
//         var levelText = itemInfoRect.transform.Find("Level")?.GetComponent<TMP_Text>();
//         if (nameText)  itemInfoTexts[-2] = nameText;
//         if (levelText) itemInfoTexts[-1] = levelText;
//         for (int i = 0; i < 8; i++)
//         {
//             var t = itemInfoRect.transform.Find($"Property_{i}")?.GetComponent<TMP_Text>();
//             if (t) itemInfoTexts[i] = t;
//         }
//         itemInfoRect.gameObject.SetActive(false);
//     }
//
//     private void OnEnable()
//     {
//         GameClient.Instance.RegisterHandler(Protocol.QueryInventory, OnQueryInventory);
//
//     }
//
//     private void OnDisable()
//     {
//         GameClient.Instance.UnregisterHandler(Protocol.QueryInventory);
//     }
//
//     public override void ShowMe<T>(Action<T> onBegin = null, Action<T> onComplete = null, bool fadeIn = false)
//     {
//         base.ShowMe(onBegin, onComplete, fadeIn);
//         InputBindService.Instance.UIIsOpen = true;
//         backpack.OnNeedMore += OnBackpackNeedMore;
//         backpack.ShowEmptyGrid();
//         SendQueryInventory(0, 300);
//     }
//
//     public override void HideMe<T>(Action<T> onBegin = null, Action<T> onComplete = null, bool fadeOut = false)
//     {
//         base.HideMe(onBegin, onComplete, fadeOut);
//         backpack.OnNeedMore -= OnBackpackNeedMore;
//         InputBindService.Instance.UIIsOpen = false;
//     }
//     
//
//     private void OnBackpackNeedMore(int lastVisibleIndex)
//     {
//         if(requesting) return;
//         int maxSize = PlayerInventory.Instance.MaxSize;
//         int nextStart = PlayerInventory.Instance.GetNextStart(batchSize);
//         
//         if(nextStart >= maxSize) return;
//
//         int endExclusive = Mathf.Min(nextStart + batchSize, maxSize);
//         SendQueryInventory(nextStart, endExclusive);
//     }
//
//     private void SendQueryInventory(int start, int endExclusive)
//     {
//         if (requesting) return;
//         int maxSize = Mathf.Max(PlayerInventory.Instance.MaxSize, endExclusive);
//         start = Mathf.Clamp(start, 0, maxSize);
//         endExclusive = Mathf.Clamp(endExclusive, 0, maxSize);
//         if (start >= endExclusive) return;
//         if (PlayerInventory.Instance.IsRangeRequested(start, endExclusive))
//             return;
//         requesting = true;
//         PlayerInventory.Instance.MarkRangeRequested(start, endExclusive);
//         GameClient.Instance.Send(Protocol.QueryInventory, 
//             new ClientPlayerQueryInventory
//         {
//             StartSlot = start,
//             EndSlot = endExclusive,
//         });
//     }
//
//     private void OnQueryInventory(GamePacket packet)
//     {
//         var data = packet.DeSerializePayload<ServerQueryInventory>();
//         PlayerInventory.Instance.SetMaxSize(data.MaxSize);
//         PlayerInventory.Instance.UpsertRange(data.Data);
//         
//         int roundedTotal = Mathf.CeilToInt((float)PlayerInventory.Instance.MaxSize / backpack.columns) * backpack.columns;
//         backpack.SetTotalAndResize(roundedTotal);
//         requesting = false;
//     }
//     
//     public void ShowItemInfo(RectTransform slotRT, ItemData data, float padding = 12f)
// {
//     if (itemInfoRect == null || data == null) return;
//
//     // 填充文本
//     SetItemInfoContent(data);
//
//     // 先激活 + 强制刷新，拿到真实尺寸
//     itemInfoRect.gameObject.SetActive(true);
//     Canvas.ForceUpdateCanvases();
//     LayoutRebuilder.ForceRebuildLayoutImmediate(itemInfoRect);
//
//     // 自动象限切 pivot + 放置
//     PositionTooltipAutoPivot(slotRT, padding);
//
//     // 越界收尾
//     ClampTooltipInsideCanvas();
// }
//
// public void HideItemInfo()
// {
//     if (itemInfoRect != null)
//         itemInfoRect.gameObject.SetActive(false);
// }
//
// // ======= 文本填充 =======
// private void SetItemInfoContent(ItemData data)
// {
//     if (itemInfoTexts.TryGetValue(-2, out var name))
//         name.text = $"{data.ItemName}({data.QuantityType})";
//
//     if (itemInfoTexts.TryGetValue(-1, out var level))
//         level.text = data.ItemType == ItemType.Equip ? ((EquipData)data).Level.ToString() : "";
//
//     for (int i = 0; i < 8; i++)
//         if (itemInfoTexts.TryGetValue(i, out var prop))
//             prop.text = ""; // 视需求填充
// }
//
// // ======= 定位（自动 pivot + 放置） =======
// private void PositionTooltipAutoPivot(RectTransform slotRT, float padding)
// {
//     var canvas = UIService.Instance.Canvas;
//     var cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;
//
//     // 槽位中心的屏幕坐标
//     Vector2 slotScreen = RectTransformUtility.WorldToScreenPoint(cam, slotRT.TransformPoint(slotRT.rect.center));
//
//     // 屏幕中心判象限
//     Rect pxRect = canvas.pixelRect;
//     Vector2 screenCenter = pxRect.center;
//     bool onRightHalf = slotScreen.x >= screenCenter.x;
//     bool onUpperHalf = slotScreen.y >= screenCenter.y;
//
//     // 象限 → pivot
//     itemInfoRect.pivot = new Vector2(onRightHalf ? 1f : 0f, onUpperHalf ? 1f : 0f);
//
//     // 展开方向 padding
//     Vector2 screenOffset = new Vector2(onRightHalf ? -padding : padding,
//                                        onUpperHalf ? -padding : padding);
//     Vector2 targetScreen = slotScreen + screenOffset;
//
//     // 转到父本地坐标并一次性设置
//     RectTransformUtility.ScreenPointToLocalPointInRectangle(
//         (RectTransform)itemInfoRect.parent, targetScreen, cam, out var targetLocal);
//     itemInfoRect.anchoredPosition = targetLocal;
// }
//
// // ======= 越界收尾 =======
// private void ClampTooltipInsideCanvas()
// {
//     var canvas = UIService.Instance.Canvas;
//     var cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;
//
//     Vector3[] corners = new Vector3[4];
//     itemInfoRect.GetWorldCorners(corners);
//     Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
//     Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
//
//     Rect bounds = canvas.pixelRect;
//     Vector2 offset = Vector2.zero;
//     if (max.x > bounds.xMax) offset.x += bounds.xMax - max.x;
//     if (min.x < bounds.xMin) offset.x += bounds.xMin - min.x;
//     if (max.y > bounds.yMax) offset.y += bounds.yMax - max.y;
//     if (min.y < bounds.yMin) offset.y += bounds.yMin - min.y;
//
//     if (offset != Vector2.zero)
//     {
//         Vector2 currentScreen = RectTransformUtility.WorldToScreenPoint(cam, itemInfoRect.position);
//         Vector2 targetScreen = currentScreen + offset;
//         RectTransformUtility.ScreenPointToLocalPointInRectangle(
//             (RectTransform)itemInfoRect.parent, targetScreen, cam, out var targetLocal);
//         itemInfoRect.anchoredPosition = targetLocal;
//     }
// }
//
//
// }
