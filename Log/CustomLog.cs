using UnityEngine;

public static class CustomLog
{
    private static LogView instance;
    private const bool OpenEditorLog = true;
    private const bool OpenCustomLog = false;
    
    public static void Initialize(LogView logView)
    {
        instance = logView;
    }
    
    public static void LogError(string message)
    {
        if(OpenEditorLog) UnityEngine.Debug.LogError(message);
        if(OpenCustomLog) instance?.AddLog(LogType.Error, message);
    }
    
    public static void Warning(string message)
    {
        if(OpenEditorLog) UnityEngine.Debug.LogWarning(message);
        if(OpenCustomLog) instance?.AddLog(LogType.Warning, message);
    }
    
    public static void Debug(string message)
    {
        if(OpenEditorLog) UnityEngine.Debug.Log(message);
        if(OpenCustomLog) instance?.AddLog(LogType.Debug, message);
    }
}