using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LogItem : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public Button expandButton;
    public GameObject stackTracePanel;
    public TextMeshProUGUI stackTraceText;
    
    private bool isExpanded = false;
    
    public void Setup(LogData logData, Color color)
    {
        messageText.text = "[" + logData.timestamp.ToString("HH:mm:ss") + "]" + logData.message;
        messageText.color = color;
        stackTraceText.text = logData.stackTrace;
        
        // 只有有堆栈信息时才显示展开按钮
        expandButton.gameObject.SetActive(!string.IsNullOrEmpty(logData.stackTrace));
        stackTracePanel.SetActive(false);
        
        expandButton.onClick.AddListener(ToggleStackTrace);
    }
    
    private void ToggleStackTrace()
    {
        isExpanded = !isExpanded;
        stackTracePanel.SetActive(isExpanded);
    }
}