using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class NotificationView : BaseView
{
    private GameObject notificationPrefab;
    private UnityEngine.RectTransform TeamNotificationRect;
    private UnityEngine.UI.Button AcceptInviteButton;
    private UnityEngine.UI.Button RefuseInviteButton;
    private TMPro.TextMeshProUGUI InviteMessageText;
    private UnityEngine.RectTransform ItemNotificationRect;

    private void BindComponent()
    {
        var root = this.transform;

        TeamNotificationRect = root.Find("TeamNotification") as RectTransform;

        AcceptInviteButton = root.Find("TeamNotification/AcceptInviteButton")?.GetComponent<UnityEngine.UI.Button>();

        RefuseInviteButton = root.Find("TeamNotification/RefuseInviteButton")?.GetComponent<UnityEngine.UI.Button>();

        InviteMessageText = root.Find("TeamNotification/InviteMessageText")?.GetComponent<TMPro.TextMeshProUGUI>();

        ItemNotificationRect = root.Find("ItemNotification") as RectTransform;

    }

    private NotificationController controller;
    
    [Header("物品通知参数")]
    public float itemHeight = 100f; 
    public float spacing = 10f;
    public float paddingTop = 20f;
    public float slideDuration = 0.5f;
    public float displayDuration = 3f;
    public float fadeDuration = 0.5f;
    public Ease slideEase = Ease.OutBack;
    public Ease fadeEase = Ease.OutBack;
    private readonly Queue<ItemNotificationUI> activeNotifications = new Queue<ItemNotificationUI>();

    protected override void Awake()
    {
        base.Awake();
        BindComponent();
        controller = new NotificationController(this);
        notificationPrefab = ResourceService.Instance.LoadResource<GameObject>("Prefabs/UI/HUD/ItemNotificationUI");
        AcceptInviteButton.onClick.AddListener(AcceptInvitation);
        RefuseInviteButton.onClick.AddListener(RefuseInvitation);
        TeamNotificationRect.gameObject.SetActive(false);
        
        PoolService.Instance.Preload(notificationPrefab, 20, (obj) =>
        {
            var rect = obj.GetComponent<RectTransform>();
            itemHeight = rect.rect.height;
            rect.sizeDelta = new Vector2(ItemNotificationRect.sizeDelta.x, itemHeight);
        });
        
        
    }
    
    private void AcceptInvitation()
    {
        controller.AcceptInvitation();
        TeamNotificationRect.gameObject.SetActive(false);
    }

    private void RefuseInvitation()
    {
        controller.RefuseInvitation();
        TeamNotificationRect.gameObject.SetActive(false);
    }

    public void ReceiveInvite(string message)
    {
        InviteMessageText.text = message;
        TeamNotificationRect.gameObject.SetActive(true);
    }

    public void AcquireItem(Sprite iconSprite, string itemName, int itemCount)
    {
        GameObject newNotification = PoolService.Instance.Spawn(notificationPrefab, ItemNotificationRect, false);
        
        // 设置通知项的基本属性
        var notificationUI = newNotification.GetComponent<ItemNotificationUI>();
        
        notificationUI.UpdateNotification( $"{itemName} * {itemCount}", iconSprite);

        // 初始化位置（屏幕左侧外部）
        float startX = -ItemNotificationRect.rect.width;
        float targetX = 0f;
        
        notificationUI.Rect.anchoredPosition = new Vector2(startX, 0);
        notificationUI.CanvasGroup.alpha = 0;
        
        activeNotifications.Enqueue(notificationUI);

        // 更新所有通知项的位置
        UpdateAllNotificationsLayout();

        // 创建进入动画
        Sequence slideInSeq = DOTween.Sequence();
        slideInSeq.Append(notificationUI.Rect.DOAnchorPosX(targetX, slideDuration).SetEase(slideEase));
        slideInSeq.Join(notificationUI.CanvasGroup.DOFade(1, slideDuration));
        slideInSeq.AppendInterval(displayDuration);
        
        // 退出动画
        slideInSeq.Append(canvasGroup.DOFade(0, fadeDuration).SetEase(fadeEase));
        slideInSeq.Join(notificationUI.Rect.DOAnchorPosX(startX, fadeDuration).SetEase(Ease.InBack));
        
        slideInSeq.OnComplete(() =>
        {
            // 移除并回收
            activeNotifications.Dequeue();
            PoolService.Instance.Despawn(newNotification);
            
            // 更新剩余通知项的位置
            UpdateAllNotificationsLayout();
        });
        slideInSeq.SetAutoKill(true);
    }
    

    // 更新所有通知项的布局位置
    private void UpdateAllNotificationsLayout()
    {
        int index = 0;
        foreach (ItemNotificationUI notification in activeNotifications)
        {
            float targetY = -((itemHeight + spacing) * index + paddingTop);
            var rect = notification.Rect;

            // 直接设置位置，不再判断动画状态（反正你只关心最终位置）
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, targetY);

            index++;
        }
    }
}
