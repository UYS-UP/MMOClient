using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatItemUI : MonoBehaviour, IPooledObject
{
    [Header("组件引用")]
    [SerializeField] private TMP_Text chatContentText;
    
    [Header("布局设置")]
    [SerializeField] private bool useContentSizeFitter = true;
    
    private RectTransform rectTransform;
    private ContentSizeFitter sizeFitter;
    private StringBuilder stringBuilder = new StringBuilder();

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // 如果没有手动指定，自动查找
        if (chatContentText == null)
        {
            chatContentText = GetComponent<TMP_Text>();
            if (chatContentText == null)
            {
                chatContentText = GetComponentInChildren<TMP_Text>();
            }
        }

        // 配置 ContentSizeFitter
        if (useContentSizeFitter)
        {
            sizeFitter = GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = gameObject.AddComponent<ContentSizeFitter>();
            }
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
    
    

    /// <summary>
    /// 更新聊天消息内容
    /// </summary>
    public void UpdateInfo(string senderName, string content, DateTime timestamp)
    {
        stringBuilder.Clear();
        stringBuilder.Append("[");
        stringBuilder.Append(timestamp.ToString("HH:mm:ss"));
        stringBuilder.Append("] ");
        stringBuilder.Append(senderName);
        stringBuilder.Append(": ");
        stringBuilder.Append(content);
       
        chatContentText.text = stringBuilder.ToString();
        
        // ✓ 不调用 ForceRebuildLayoutImmediate
        // Unity 的 UI 系统会在下一帧自动处理布局更新
        // InfiniteScrollView 会通过 MonitorItemHeight 检测高度变化
    }

    /// <summary>
    /// 获取首选高度（不触发强制刷新）
    /// </summary>
    public float GetPreferredHeight()
    {
        if (chatContentText == null) return 0f;
        return LayoutUtility.GetPreferredHeight(rectTransform);
    }

    /// <summary>
    /// 对象从池中取出时调用
    /// </summary>
    public void OnObjectSpawn()
    {
    }

    /// <summary>
    /// 对象返回池中时调用
    /// </summary>
    public void OnObjectDespawn()
    {
        // 清理内容
        stringBuilder.Clear();
        if (chatContentText != null)
        {
            chatContentText.text = "";
        }
    }
}

