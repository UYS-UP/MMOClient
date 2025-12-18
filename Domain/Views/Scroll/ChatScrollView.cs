using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 聊天滚动视图 - 带 DOTween 平滑滚动动画
/// 
/// 新增功能：
/// - 使用 DOTween 实现平滑滚动到底部
/// - 可配置的动画时长和缓动曲线
/// - 自动取消上一个动画，避免冲突
/// </summary>
public class ChatScrollView : InfiniteScrollView<ChatMessageData>
{
    [SerializeField] private GameObject prefab;
    [Header("聊天特定设置")] 
    [SerializeField] private bool autoScrollToBottom = true;
    [SerializeField] private float autoScrollThreshold = 50f; // 距离底部多少像素内才自动滚动

    [Header("滚动动画设置")]
    [SerializeField] private float scrollDuration = 0.3f;      // 滚动动画时长（秒）
    [SerializeField] private Ease scrollEase = Ease.OutQuad;   // 缓动曲线
    [SerializeField] private bool useUnscaledTime = false;     // 是否使用不受时间缩放影响的时间

    private bool wasAtBottom = true;
    private Tweener currentScrollTween;

    protected override void Awake()
    {
        base.Awake();
        PoolService.Instance.Preload(prefab, warmupCount);
        GetPrefabForItem += (node) => prefab;
    }

    /// <summary>
    /// 重写 AddItem，添加平滑滚动到底部功能
    /// </summary>
    public override void AddItem(ChatMessageData data)
    {
        // 检查是否在底部
        if (autoScrollToBottom)
        {
            wasAtBottom = IsNearBottom();
        }

        // 调用基类方法（增量添加）
        base.AddItem(data);

        // 如果之前在底部，添加后平滑滚动到底部
        if (autoScrollToBottom && wasAtBottom)
        {
            StartCoroutine(SmoothScrollToBottomNextFrame());
        }
    }

    /// <summary>
    /// 检查是否接近底部
    /// </summary>
    private bool IsNearBottom()
    {
        if (content == null || viewport == null) return true;

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;
        float scrollY = content.anchoredPosition.y;

        // 如果内容高度小于视口高度，认为在底部
        if (contentHeight <= viewportHeight) return true;

        float maxScroll = contentHeight - viewportHeight;
        float distanceFromBottom = maxScroll - scrollY;

        return distanceFromBottom <= autoScrollThreshold;
    }

    /// <summary>
    /// 平滑滚动到底部（延迟到下一帧，等待布局更新）
    /// </summary>
    private IEnumerator SmoothScrollToBottomNextFrame()
    {
        yield return null;
        yield return null; // 等待两帧，确保布局更新完成

        SmoothScrollToBottom();
    }

    /// <summary>
    /// 平滑滚动到底部（使用 DOTween）
    /// </summary>
    public void SmoothScrollToBottom()
    {
        if (content == null || viewport == null) return;

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        // 如果内容高度小于视口高度，不需要滚动
        if (contentHeight <= viewportHeight)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
            return;
        }

        // 计算目标位置
        float targetY = contentHeight - viewportHeight;
        float currentY = content.anchoredPosition.y;

        // 如果已经在底部，不需要动画
        if (Mathf.Abs(targetY - currentY) < 1f)
        {
            return;
        }

        // 取消之前的滚动动画
        if (currentScrollTween != null && currentScrollTween.IsActive())
        {
            currentScrollTween.Kill();
        }
        
        // 创建新的滚动动画
        currentScrollTween = content.DOAnchorPosY(targetY, scrollDuration)
            .SetEase(scrollEase)
            .SetUpdate(useUnscaledTime)
            .OnComplete(() => 
            {
                currentScrollTween = null;
            });
    }

    /// <summary>
    /// 设置自动滚动
    /// </summary>
    public void SetAutoScroll(bool enable)
    {
        autoScrollToBottom = enable;
    }

    /// <summary>
    /// 设置滚动动画参数
    /// </summary>
    public void SetScrollAnimation(float duration, Ease ease)
    {
        scrollDuration = Mathf.Max(0.1f, duration);
        scrollEase = ease;
    }

    /// <summary>
    /// 停止当前滚动动画
    /// </summary>
    public void StopScrollAnimation()
    {
        if (currentScrollTween != null && currentScrollTween.IsActive())
        {
            currentScrollTween.Kill();
            currentScrollTween = null;
        }
    }

    /// <summary>
    /// 重写高度估算，根据消息内容长度预估
    /// </summary>
    protected override float EstimateItemHeight(ChatMessageData data)
    {
        // 基础高度
        float baseHeight = 40f;

        // 根据内容长度预估行数
        if (!string.IsNullOrEmpty(data.Content))
        {
            // 假设每行约 30 个字符，每行高度约 20px
            int estimatedLines = Mathf.CeilToInt(data.Content.Length / 30f);
            estimatedLines = Mathf.Max(1, estimatedLines);
            
            float estimatedHeight = baseHeight + (estimatedLines - 1) * 20f;
            return ClampItemHeight(estimatedHeight);
        }

        return ClampItemHeight(baseHeight);
    }

    /// <summary>
    /// 清理时停止动画
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable();
        StopScrollAnimation();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopScrollAnimation();
    }

    private void Log(string message)
    {
        if (enableDebugLog)
            Debug.Log($"[ChatScrollView] {message}");
    }
}

