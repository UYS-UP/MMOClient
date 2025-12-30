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
    public float itemHeight = 40f;
    public bool usePrefabHeight = true;
    public float spacing = 10f;
    public float paddingTop = 20f;
    public float slideDuration = 0.5f;
    public float displayDuration = 3f;
    public float fadeDuration = 0.5f;
    public Ease slideEase = Ease.OutBack;
    public Ease fadeEase = Ease.OutBack;
    private readonly Queue<ItemAcquiredNotifiycation> activeNotifications = new Queue<ItemAcquiredNotifiycation>();

    protected override void Awake()
    {
        base.Awake();
        BindComponent();
        controller = new NotificationController(this);
        notificationPrefab = ResourceService.Instance.LoadResource<GameObject>("Prefabs/UI/Modules/Notification/ItemAcquiredNotification");
        AcceptInviteButton.onClick.AddListener(AcceptInvitation);
        RefuseInviteButton.onClick.AddListener(RefuseInvitation);
        TeamNotificationRect.gameObject.SetActive(false);
        if(usePrefabHeight) itemHeight = notificationPrefab.GetComponent<RectTransform>().sizeDelta.y;
        PoolService.Instance.Preload(notificationPrefab, 20, (obj) =>
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(ItemNotificationRect.rect.width, rect.sizeDelta.y);
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
        var notificationUI = newNotification.GetComponent<ItemAcquiredNotifiycation>();
        
        notificationUI.UpdateNotification( $"{itemName} * {itemCount}", iconSprite);

        // 初始化位置（屏幕左侧外部）
        float startX = -ItemNotificationRect.rect.width;
        float targetX = 0f;
        
        notificationUI.Rect.anchoredPosition = new Vector2(startX, 0);
        notificationUI.CanvasGroup.alpha = 0;
        
        activeNotifications.Enqueue(notificationUI);
        
        UpdateAllNotificationsLayout();
        
        Sequence slideInSeq = DOTween.Sequence();
        slideInSeq.Append(notificationUI.Rect.DOAnchorPosX(targetX, slideDuration).SetEase(slideEase));
        slideInSeq.Join(notificationUI.CanvasGroup.DOFade(1, slideDuration));
        slideInSeq.AppendInterval(displayDuration);
        
        slideInSeq.Append(notificationUI.CanvasGroup.DOFade(0, fadeDuration).SetEase(fadeEase));
        slideInSeq.Join(notificationUI.Rect.DOAnchorPosX(startX, fadeDuration).SetEase(Ease.InBack));
        
        slideInSeq.OnComplete(() =>
        {
            activeNotifications.Dequeue();
            PoolService.Instance.Despawn(newNotification);
            
            UpdateAllNotificationsLayout();
        });
        slideInSeq.SetAutoKill(true);
    }
    
    
    private void UpdateAllNotificationsLayout()
    {
        int index = 0;
        foreach (ItemAcquiredNotifiycation notification in activeNotifications)
        {
            float targetY = -((itemHeight + spacing) * index + paddingTop);
            var rect = notification.Rect;
            
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, targetY);

            index++;
        }
    }
}
