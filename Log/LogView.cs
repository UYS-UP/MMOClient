using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using UnityEngine.Events;

public enum LogType
{
    Error,
    Warning,
    Debug
}

public class LogView : BaseView
{
    [Header("UI References")]
    public Button errorButton;
    public Button warningButton;
    public Button debugButton;
    public Button closeButton;
    public Transform logContent;
    public GameObject logItemPrefab;
    public ScrollRect scrollRect;
    
    [Header("Settings")]
    public int maxLogCount = 1000;
    public bool autoScroll = true;
    public float uiUpdateInterval = 0.1f; // UI更新间隔
    
    // 日志存储
    private Dictionary<LogType, List<LogData>> logs = new Dictionary<LogType, List<LogData>>();
    private LogType currentDisplayType = LogType.Error;
    
    // 对象池
    private Queue<GameObject> logItemPool = new Queue<GameObject>();
    private List<GameObject> activeLogItems = new List<GameObject>();
    private const int POOL_SIZE = 50;
    
    // 批量处理
    private bool needsUIUpdate = false;
    private Coroutine updateCoroutine;
    private Queue<LogData> pendingLogs = new Queue<LogData>();
    
    // 日志项颜色
    private readonly Color errorColor = new Color(1f, 0.2f, 0.2f);
    private readonly Color warningColor = new Color(1f, 0.8f, 0.2f);
    private readonly Color debugColor = new Color(0.2f, 0.8f, 1f);
    
    protected override void Awake()
    {
        base.Awake();
        
        // 初始化日志字典
        foreach (LogType type in System.Enum.GetValues(typeof(LogType)))
        {
            logs[type] = new List<LogData>();
        }
        
        // 初始化对象池
        InitializeObjectPool();
        
        // 绑定按钮事件
        errorButton.onClick.AddListener(() => SwitchLogType(LogType.Error));
        warningButton.onClick.AddListener(() => SwitchLogType(LogType.Warning));
        debugButton.onClick.AddListener(() => SwitchLogType(LogType.Debug));
        closeButton.onClick.AddListener(() =>
        {
            closeButton.interactable = false;
            scrollRect.gameObject.SetActive(false);
            StopUpdateCoroutine();
        });
        scrollRect.gameObject.SetActive(false);
    }
    
    private void InitializeObjectPool()
    {
        for (int i = 0; i < POOL_SIZE; i++)
        {
            GameObject logItem = Instantiate(logItemPrefab, logContent);
            logItem.SetActive(false);
            logItemPool.Enqueue(logItem);
        }
    }
    
    private GameObject GetPooledLogItem()
    {
        if (logItemPool.Count > 0)
        {
            return logItemPool.Dequeue();
        }
        
        // 如果池子空了，动态创建一个
        GameObject newItem = Instantiate(logItemPrefab, logContent);
        return newItem;
    }
    
    private void ReturnLogItemToPool(GameObject logItem)
    {
        logItem.SetActive(false);
        logItemPool.Enqueue(logItem);
    }
    
    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        StartUpdateCoroutine();
    }
    
    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
        StopUpdateCoroutine();
    }
    
    private void StartUpdateCoroutine()
    {
        if (updateCoroutine == null)
        {
            updateCoroutine = StartCoroutine(UpdateUIPeriodically());
        }
    }
    
    private void StopUpdateCoroutine()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }
    }
    
    private IEnumerator UpdateUIPeriodically()
    {
        while (true)
        {
            if (needsUIUpdate && scrollRect.gameObject.activeInHierarchy)
            {
                UpdateLogDisplay();
                needsUIUpdate = false;
            }
            yield return new WaitForSeconds(uiUpdateInterval);
        }
    }
    
    private void HandleLog(string logString, string stackTrace, UnityEngine.LogType type)
    {
        LogType logType = ConvertToCustomLogType(type);
        AddLog(logType, logString, stackTrace);
    }
    
    private LogType ConvertToCustomLogType(UnityEngine.LogType unityLogType)
    {
        switch (unityLogType)
        {
            case UnityEngine.LogType.Error:
            case UnityEngine.LogType.Exception:
            case UnityEngine.LogType.Assert:
                return LogType.Error;
            case UnityEngine.LogType.Warning:
                return LogType.Warning;
            default:
                return LogType.Debug;
        }
    }
    
    public void AddLog(LogType type, string message, string stackTrace = "")
    {
        var logData = new LogData
        {
            type = type,
            message = message,
            stackTrace = stackTrace,
            timestamp = System.DateTime.Now
        };
        
        // 添加到对应类型的日志列表
        logs[type].Add(logData);
        
        // 限制日志数量
        if (logs[type].Count > maxLogCount)
        {
            logs[type].RemoveAt(0);
        }
        
        // 批量处理：将日志加入待处理队列
        lock (pendingLogs)
        {
            pendingLogs.Enqueue(logData);
        }
        
        // 标记需要UI更新，但不会立即执行
        if (type == currentDisplayType)
        {
            needsUIUpdate = true;
        }
    }
    
    private void SwitchLogType(LogType type)
    {
        currentDisplayType = type;
        
        // 切换类型时立即更新
        UpdateLogDisplayImmediate();
        
        UpdateButtonStates(type);
        scrollRect.gameObject.SetActive(true);
        closeButton.interactable = true;
        StartUpdateCoroutine();
    }
    
    private void UpdateLogDisplayImmediate()
    {
        ClearActiveLogItems();
        
        foreach (var logData in logs[currentDisplayType])
        {
            CreateLogItemImmediate(logData);
        }
        
        if (autoScroll)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    private void UpdateLogDisplay()
    {
        if (!scrollRect.gameObject.activeInHierarchy) return;
        
        // 批量处理待显示的日志
        lock (pendingLogs)
        {
            while (pendingLogs.Count > 0)
            {
                var logData = pendingLogs.Dequeue();
                if (logData.type == currentDisplayType)
                {
                    CreateLogItem(logData);
                }
            }
        }
        
        if (autoScroll)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    private void CreateLogItemImmediate(LogData logData)
    {
        var logItem = GetPooledLogItem();
        var logItemComponent = logItem.GetComponent<LogItem>();
        
        if (logItemComponent != null)
        {
            logItemComponent.Setup(logData, GetColorForLogType(logData.type));
        }
        
        logItem.SetActive(true);
        activeLogItems.Add(logItem);
    }
    
    private void CreateLogItem(LogData logData)
    {
        var logItem = GetPooledLogItem();
        var logItemComponent = logItem.GetComponent<LogItem>();
        
        if (logItemComponent != null)
        {
            logItemComponent.Setup(logData, GetColorForLogType(logData.type));
        }
        
        logItem.SetActive(true);
        activeLogItems.Add(logItem);
        
        // 限制显示的日志项数量
        if (activeLogItems.Count > maxLogCount / 2)
        {
            var oldestItem = activeLogItems[0];
            activeLogItems.RemoveAt(0);
            ReturnLogItemToPool(oldestItem);
        }
    }
    
    private void ClearActiveLogItems()
    {
        foreach (var logItem in activeLogItems)
        {
            ReturnLogItemToPool(logItem);
        }
        activeLogItems.Clear();
        
        // 清空待处理队列
        lock (pendingLogs)
        {
            pendingLogs.Clear();
        }
    }
    
    private Color GetColorForLogType(LogType type)
    {
        switch (type)
        {
            case LogType.Error: return errorColor;
            case LogType.Warning: return warningColor;
            case LogType.Debug: return debugColor;
            default: return Color.white;
        }
    }
    
    private void UpdateButtonStates(LogType selectedType)
    {
        errorButton.image.color = selectedType == LogType.Error ? Color.gray : Color.white;
        warningButton.image.color = selectedType == LogType.Warning ? Color.gray : Color.white;
        debugButton.image.color = selectedType == LogType.Debug ? Color.gray : Color.white;
    }
    
    // 清空日志
    public void ClearLogs(LogType? type = null)
    {
        if (type.HasValue)
        {
            logs[type.Value].Clear();
        }
        else
        {
            foreach (var logList in logs.Values)
            {
                logList.Clear();
            }
        }
        
        ClearActiveLogItems();
        needsUIUpdate = false;
    }
    
    // 手动立即更新UI（用于特殊情况）
    public void ForceUpdateUI()
    {
        UpdateLogDisplayImmediate();
    }
}

[System.Serializable]
public struct LogData
{
    public LogType type;
    public string message;
    public string stackTrace;
    public System.DateTime timestamp;
}