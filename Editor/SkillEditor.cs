using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor.Animations;


#if UNITY_EDITOR

public class SkillEditor : EditorWindow
{
    
    private readonly string[] validEventTypes = new string[] 
    { 
        nameof(AnimationEvent), 
        nameof(EffectEvent), 
    };

    private readonly string[] validPhaseTypes = new string[] 
    { 
        nameof(OpenComboWindowPhase),
        nameof(MoveStepPhase)
    };
    
    private const string PREFS_JSON_PATH_KEY = "SkillEditor_JsonPath";
    private const string ANIMATION_ROOT_PATH = "Assets/ArtRes/Animations/"; 
    private const string EFFECT_ROOT_PATH = "Assets/Resources/Prefabs/Effect/";
    
    const float TIMELINE_HEADER_HEIGHT = 30f;
    const float TRACK_AREA_HEIGHT = 120f;
    const float PIXELS_PER_SECOND = 100f;
    private const float TRACK_HEIGHT = 30f;
    private const float TRACK_SPACING = 2f;
    private const float HANDLE_WIDTH = 6f;

    private string jsonFilePath = "";
    private List<SkillTimelineConfig> skillConfigs;
    private int selectedSkillIndex = 0;
    private string[] skillOptions;
    private bool isDataLoaded = false;
    
    
    [SerializeField] private GameObject previewObject;
    
    
    private Vector2 timelineScrollPosition;
    private Vector2 inspectorScrollPos;
    private float timelineZoom = 1f;
    private readonly float[] zoomLevels = { 0.5f, 1f, 5f, 10f };
    private int currentZoomIndex = 2;
    
    private enum DragMode { None, MovePhase, ResizeStart, ResizeEnd, MoveEvent }
    private DragMode currentDragMode = DragMode.None;
    private object activeItem = null;
    private float dragItemStartTime;
    private float dragItemDuration;
    private float dragTimeOffset; 
    
    private float currentTime = 0f;
    private bool isPlaying = false;
    private bool isLooping = false;
    private double lastFrameTime;
    private bool isDraggingPlayhead = false;

    private SkillPreviewSystem previewSystem;
    
    // 预览（Playables）
    private PlayableGraph previewGraph;
    private AnimationMixerPlayable animMixer;
    private AnimationPlayableOutput animOutput;
    private Playable currentPlayable;
    private Playable nextPlayable;
    private bool isBlending = false;
    private float blendElapsed = 0f;
    private float blendDuration = 0.25f;
    private bool autoCombo = true;
    private bool isAutoCombo = false; // 【新增】是否开启自动连击预览
    private bool _hasTriggeredCombo = false; //防止同一帧多次触发
        
    [MenuItem("Tools/Skill Editor")]
    static void OpenWindow()
    {
        var window = GetWindow<SkillEditor>("Skill Editor");
        window.Show();
    }

    private void OnEnable()
    {
        previewSystem = new SkillPreviewSystem();

        string savedPath = EditorPrefs.GetString(PREFS_JSON_PATH_KEY, "");
        if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
        {
            jsonFilePath = savedPath;
            LoadJsonFile();
        }
        
        EditorApplication.update += OnEditorUpdate;
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        SceneView.duringSceneGui -= OnSceneGUI;
        previewSystem?.Cleanup();
    }
    
    private void OnEditorUpdate()
    {
        double timeSinceStartup = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(timeSinceStartup - lastFrameTime);
        lastFrameTime = timeSinceStartup;

        if (isPlaying && isDataLoaded && skillConfigs != null && selectedSkillIndex < skillConfigs.Count)
        {
            var skill = skillConfigs[selectedSkillIndex];
            if (isAutoCombo)
            {
                CheckAndTriggerCombo(skill, currentTime);
                skill = skillConfigs[selectedSkillIndex]; 
            }
            currentTime += deltaTime;

            // 循环/结束处理
            if (currentTime >= skill.Duration)
            {
                if (isLooping) currentTime = 0;
                else
                {
                    currentTime = skill.Duration;
                    isPlaying = false;
                }
            }
            
            UpdatePreview(currentTime); 
            Repaint();
        }
    }
    
    
    private void UpdatePreview(float time)
    {
        if (previewObject == null || !isDataLoaded || skillConfigs == null) return;
        previewSystem.BindTarget(previewObject);
        previewSystem.Sample(skillConfigs[selectedSkillIndex], time);
    }
    
    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isDataLoaded || activeItem == null || previewObject == null || previewSystem == null) 
            return;
        
        previewSystem.DrawSceneGUI(activeItem);
        
        if (Event.current.type == EventType.MouseDrag)
        {
            Repaint(); 
            UpdatePreview(currentTime);
        }
    }
    
    private void CheckAndTriggerCombo(SkillTimelineConfig currentSkill, float time)
    {
        if (_hasTriggeredCombo) return;
        
        foreach (var phase in currentSkill.ClientPhases)
        {
            if (phase is OpenComboWindowPhase comboPhase)
            {
                if (time >= comboPhase.StartTime && comboPhase.Next > 0)
                {
                    SwitchToSkill(comboPhase.Next);
                    return;
                }
            }
        }
    }
    
    private void SwitchToSkill(int nextSkillId)
    {
        int nextIndex = skillConfigs.FindIndex(s => s.Id == nextSkillId);
        if (nextIndex != -1)
        {
            selectedSkillIndex = nextIndex;
            currentTime = 0f;
            _hasTriggeredCombo = false;
            
            Debug.Log($"[Auto Combo] Switch to Skill ID: {nextSkillId}");

            UpdatePreview(0f);
        }
        else
        {
            Debug.LogWarning($"Combo target skill id {nextSkillId} not found!");
        }
    }
    

    void OnGUI()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.S && Event.current.control)
        {
            SaveToFile();
            Event.current.Use();
        }
        
        float windowHeight = position.height;
        float windowWidth = position.width;
        float leftWidth = windowWidth * 0.2f;
        float rightWidth = windowWidth * 0.25f;
        float middleWidth = windowWidth - leftWidth - rightWidth;
        
        GUILayout.BeginHorizontal();
        
        GUILayout.BeginVertical(GUILayout.Width(leftWidth));
        DrawLeftArea(leftWidth);
        GUILayout.EndVertical();
        
        GUILayout.BeginVertical(GUILayout.Width(middleWidth));
        DrawMiddleTimelineArea(middleWidth);
        GUILayout.EndVertical();
        
        GUILayout.BeginVertical(GUILayout.Width(rightWidth));
        DrawInspectorArea();
        GUILayout.EndVertical();
        
        GUILayout.EndHorizontal();



    }


    private void DrawLeftArea(float leftAreaWidth)
    {
        GUILayout.Label("Skill Info", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("JSON File:", GUILayout.Width(80));
        jsonFilePath = EditorGUILayout.TextField(jsonFilePath, GUILayout.Width(leftAreaWidth - 60));
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("Select JSON file", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                jsonFilePath = path;
                LoadJsonFile();
                GUI.FocusControl(null);
            }
        }
        
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(PREFS_JSON_PATH_KEY, jsonFilePath);
        }

        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("PreviewObject:", GUILayout.Width(80));
        previewObject = (GameObject)EditorGUILayout.ObjectField(previewObject,  typeof(GameObject), true, GUILayout.Width(leftAreaWidth));
        GUILayout.EndHorizontal();
        
        // 如果没有加载数据，显示提示
        if (!isDataLoaded || skillConfigs == null || skillConfigs.Count == 0)
        {
            EditorGUILayout.HelpBox("Please select a valid JSON file to load skill configurations.", MessageType.Info);
            return;
        }

        GUILayout.Space(10);

        // 技能选择下拉框
        GUILayout.Label("Select Skill:", EditorStyles.boldLabel);
        selectedSkillIndex = EditorGUILayout.Popup(selectedSkillIndex, skillOptions);

        if (selectedSkillIndex >= 0 && selectedSkillIndex < skillConfigs.Count)
        {
            var selectedSkill = skillConfigs[selectedSkillIndex];
            
            GUILayout.Space(10);
            
            // 显示技能详细信息
            GUILayout.Label("Skill Details", EditorStyles.boldLabel);
            GUILayout.BeginVertical();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Skill Id:", GUILayout.Width(100));
            selectedSkill.Id = EditorGUILayout.IntField(selectedSkill.Id);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Skill Name:", GUILayout.Width(100));
            selectedSkill.Name = EditorGUILayout.TextField(selectedSkill.Name);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Skill Duration:", GUILayout.Width(100));
            selectedSkill.Duration = EditorGUILayout.FloatField(selectedSkill.Duration);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            GUILayout.Space(20);
            GUILayout.Label("Playback Control",  EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Time:", GUILayout.Width(100));
            float newTime = EditorGUILayout.FloatField(currentTime);
            if (Mathf.Abs(newTime - currentTime) > 0.001f)
            {
                currentTime = Mathf.Max(0, newTime);
                UpdatePreview(currentTime);
                Repaint();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Loop:", GUILayout.Width(100));
            isLooping = EditorGUILayout.Toggle(isLooping);
            GUILayout.Label("Auto Combo:", GUILayout.Width(100));
            isAutoCombo = EditorGUILayout.Toggle(isAutoCombo);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Event", GUILayout.Width(100)))
            {
                ShowAddMenu(false);
            }
            if (GUILayout.Button("Add Phase", GUILayout.Width(100)))
            {
                ShowAddMenu(true);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);

            // 3. 播放控制按钮
            GUILayout.BeginHorizontal();
        
            // Replay (重置到0并播放)
            if (GUILayout.Button("Replay", GUILayout.Width(100)))
            {
                currentTime = 0;
                isPlaying = true;
                lastFrameTime = EditorApplication.timeSinceStartup;
            }
            
            string playIcon = isPlaying ? "Pause" : "Play";
            if (GUILayout.Button(playIcon, GUILayout.Width(100)))
            {
                isPlaying = !isPlaying;
                if (isPlaying)
                {
                    lastFrameTime = EditorApplication.timeSinceStartup;
                }
            }

            // Stop (归零并停止)
            if (GUILayout.Button("Stop", GUILayout.Width(100)))
            {
                isPlaying = false;
                currentTime = 0;
                UpdatePreview(0); // 归零
            }
        
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            
            

        }
        
        void ShowAddMenu(bool isPhase)
        {
            GenericMenu menu = new GenericMenu();
            string[] types = isPhase ? validPhaseTypes : validEventTypes;

            foreach (string type in types)
            {
                // 添加 Client 选项
                menu.AddItem(new GUIContent($"Client/{type}"), false, () => OnAddObject(isPhase, true, type));
                // 添加 Server 选项
                menu.AddItem(new GUIContent($"Server/{type}"), false, () => OnAddObject(isPhase, false, type));
            }

            menu.ShowAsContext();
        }
        
        
        void OnAddObject(bool isPhase, bool isClient, string type)
        {
            if (selectedSkillIndex < 0 || selectedSkillIndex >= skillConfigs.Count) return;
            var skill = skillConfigs[selectedSkillIndex];
            
            Undo.RecordObject(this, "Add Timeline Object"); 

            if (isPhase)
            {
                var newPhase = CreatePhaseInstance(type);
                newPhase.StartTime = currentTime;
                newPhase.EndTime = currentTime + 0.5f; // 默认给 0.5s 长度

                if (isClient) skill.ClientPhases.Add(newPhase);
                else skill.ServerPhases.Add(newPhase);
            
                activeItem = newPhase; // 自动选中新创建的对象
            }
            else
            {
                // 创建 Event
                var newEvent = CreateEventInstance(type);
                newEvent.Time = currentTime;

                if (isClient) skill.ClientEvents.Add(newEvent);
                else skill.ServerEvents.Add(newEvent);

                activeItem = newEvent; // 自动选中
            }

            Repaint();
        }
        
        

        GUILayout.Space(10);

        // 保存按钮
        if (GUILayout.Button("Save", GUILayout.Height(30)))
        {
            SaveToFile();
        }
    }
    
    
    void DrawMiddleTimelineArea(float areaWidth)
    {
        GUILayout.Label("Timeline", EditorStyles.boldLabel);

        if (!isDataLoaded) return;
        var skill = skillConfigs[selectedSkillIndex];
        
        Rect inputRect = new Rect(position.width * 0.2f, 0, areaWidth, position.height);
        HandleTimelineZoom(inputRect);
        float displayDuration = Mathf.Max(Mathf.Ceil(skill.Duration), 1.0f); 
        float pixelsPerSecond = PIXELS_PER_SECOND * timelineZoom;
        Rect headerRect = GUILayoutUtility.GetRect(areaWidth, TIMELINE_HEADER_HEIGHT);
        Rect scrollRect = GUILayoutUtility.GetRect(areaWidth, TRACK_AREA_HEIGHT, GUILayout.ExpandHeight(true));
        float contentWidth = Mathf.Max(areaWidth, displayDuration * pixelsPerSecond + 200f);
        

        DrawUnityStyleHeader(headerRect, contentWidth, displayDuration);
        
       
        timelineScrollPosition = GUI.BeginScrollView(
            scrollRect, 
            timelineScrollPosition, 
            new Rect(0, 0, contentWidth, TRACK_AREA_HEIGHT),
            false, 
            false
        );
        
        Rect trackTotalRect = new Rect(0, 0, contentWidth, TRACK_AREA_HEIGHT);
        DrawGridLines(trackTotalRect, displayDuration);
        DrawTimelineTracks(trackTotalRect, skill);
        HandleDragLogic();
        DrawPlayhead(new Rect(0, 0, contentWidth, 2000), contentWidth);
        GUI.EndScrollView();
    }
    
    
    void DrawPlayhead(Rect timelineRect, float contentWidth)
    {
        float pixelsPerSecond = PIXELS_PER_SECOND * timelineZoom;
        
        // 计算播放头的 X 坐标 (相对于 ScrollView 内容左上角)
        float playheadX = currentTime * pixelsPerSecond;
        Color playheadColor = new Color(1f, 0.3f, 0.3f, 0.9f); // 鲜红色
        EditorGUI.DrawRect(new Rect(playheadX, 0, 1, timelineRect.height), playheadColor);
        float handleSize = 10f;
        Rect handleRect = new Rect(playheadX - handleSize / 2, 0, handleSize, 20); // 高度20覆盖在Header上

        EditorGUI.DrawRect(handleRect, playheadColor);
        GUIStyle labelStyle = new GUIStyle(EditorStyles.whiteMiniLabel);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(handleRect, "▼", labelStyle);
        ProcessPlayheadEvents(handleRect, timelineRect, pixelsPerSecond);
    }

    void ProcessPlayheadEvents(Rect handleRect, Rect timelineRect, float pixelsPerSecond)
    {
        Event e = Event.current;
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && (handleRect.Contains(e.mousePosition) || (e.mousePosition.y < TIMELINE_HEADER_HEIGHT && e.mousePosition.x <= timelineRect.width)))
                {
                    isDraggingPlayhead = true;
                    isPlaying = false;
                    float mouseTime = e.mousePosition.x / pixelsPerSecond;
                    currentTime = Mathf.Max(0, mouseTime);
                    GUI.FocusControl(null);
                    
                    UpdatePreview(currentTime);
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (isDraggingPlayhead)
                {
                    float mouseTime = e.mousePosition.x / pixelsPerSecond;
                    currentTime = Mathf.Max(0, mouseTime);
                    
                    UpdatePreview(currentTime);
                    
                    e.Use();
                    Repaint();
                }
                break;

            case EventType.MouseUp:
                if (isDraggingPlayhead)
                {
                    isDraggingPlayhead = false;
                    e.Use();
                }
                break;
        }
    }
    
    
    
    
    
    void DrawUnityStyleHeader(Rect rect, float contentWidth, float displayDuration)
    {

        GUI.BeginGroup(rect);
        
        EditorGUI.DrawRect(new Rect(0, 0, rect.width, rect.height), new Color(0.18f, 0.18f, 0.18f));
        EditorGUI.DrawRect(new Rect(0, rect.height - 1, rect.width, 1), new Color(0.1f, 0.1f, 0.1f));

        float pixelsPerSecond = PIXELS_PER_SECOND * timelineZoom;
        
        float step = 1.0f;
        if (pixelsPerSecond > 3000) step = 0.01f;
        else if (pixelsPerSecond > 300) step = 0.1f;
        else if (pixelsPerSecond > 50) step = 0.5f;

        int subSteps = Mathf.RoundToInt(1.0f / step);

        // 计算循环范围 (保持不变)
        int startTickIndex = Mathf.FloorToInt((timelineScrollPosition.x / pixelsPerSecond) / step);
        int endTickIndex = Mathf.CeilToInt(((timelineScrollPosition.x + rect.width) / pixelsPerSecond) / step);

        GUIStyle timeLabelStyle = new GUIStyle(EditorStyles.miniLabel);
        timeLabelStyle.alignment = TextAnchor.UpperLeft;
        timeLabelStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

        for (int i = startTickIndex; i <= endTickIndex; i++)
        {
            float t = i * step;
            
            float x = t * pixelsPerSecond - timelineScrollPosition.x;
            if (x < -40 || x > rect.width + 40) continue;
            bool isMinute = (step >= 1.0f) ? (i % 60 == 0) : (i % (subSteps * 60) == 0);
            bool isSecond = (i % subSteps == 0);
            bool isHalfSecond = (subSteps >= 2) && (i % (subSteps / 2) == 0);

            float height = 4;
            Color tickColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
            bool drawText = false;

            if (isMinute)
            {
                height = 20;
                tickColor = new Color(1f, 1f, 1f, 0.9f);
                drawText = true;
            }
            else if (isSecond)
            {
                height = 12;
                tickColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);
                drawText = true;
            }
            else if (isHalfSecond && pixelsPerSecond > 50)
            {
                height = 8;
                tickColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
                if (pixelsPerSecond > 150) drawText = true;
            }
            else if (pixelsPerSecond > 300)
            {
                height = 5;
                tickColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
            }
            
            EditorGUI.DrawRect(new Rect(x, rect.height - height, 1, height), tickColor);

            if (drawText)
            {
                string label = isSecond ? t.ToString("F0") : t.ToString("F1");
                if (Mathf.Abs(t) < 0.001f) label = "0";

                GUI.Label(new Rect(x + 3, rect.height - 20, 40, 20), label, timeLabelStyle);
            }
        }
        
        GUI.EndGroup();
    }
    
    void DrawGridLines(Rect rect, float displayDuration)
    {
        float pixelsPerSecond = PIXELS_PER_SECOND * timelineZoom;
        
        float step = 1.0f;
        if (pixelsPerSecond > 300) step = 0.1f;
        else if (pixelsPerSecond > 50) step = 0.5f;

        int subSteps = Mathf.RoundToInt(1.0f / step);
        
        float startPixel = timelineScrollPosition.x;
        float endPixel = startPixel + position.width;

        int startTickIndex = Mathf.FloorToInt((startPixel / pixelsPerSecond) / step);
        int endTickIndex = Mathf.CeilToInt((endPixel / pixelsPerSecond) / step);

        Color majorLineColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
        Color minorLineColor = new Color(0.2f, 0.2f, 0.2f, 0.1f);

        for (int i = startTickIndex; i <= endTickIndex; i++)
        {
            float t = i * step;
            float x = t * pixelsPerSecond;
            
            if (t > displayDuration + 1f) break;

            bool isSecond = (i % subSteps == 0);
            
            if (isSecond)
            {
                EditorGUI.DrawRect(new Rect(x, 0, 1, rect.height), majorLineColor);
            }
            else
            {
                EditorGUI.DrawRect(new Rect(x, 0, 1, rect.height), minorLineColor);
            }
        }
    }
    void HandleTimelineZoom(Rect area)
    {
        Event e = Event.current;
        
        if (area.Contains(e.mousePosition))
        {

            if (e.type == EventType.ScrollWheel && e.control)
            {
                float scroll = e.delta.y;

                if (scroll > 0)
                {
                    currentZoomIndex = Mathf.Max(currentZoomIndex - 1, 0);
                }
                else
                {
                    currentZoomIndex = Mathf.Min(currentZoomIndex + 1, zoomLevels.Length - 1);
                }
                
                timelineZoom = zoomLevels[currentZoomIndex];
  
                e.Use(); 
            }
        }
    }



    void DrawTimelineTracks(Rect rect, SkillTimelineConfig skill)
    {
        var clientPhaseGroups = skill.ClientPhases.GroupBy(s => s.GetType()).ToDictionary(g => g.Key.Name, g => g.ToList());
        var clientEventGroups = skill.ClientEvents.GroupBy(s => s.GetType()).ToDictionary(g => g.Key.Name, g => g.ToList());
        var serverPhaseGroups = skill.ServerPhases.GroupBy(s => s.GetType()).ToDictionary(g => g.Key.Name, g => g.ToList());
        var serverEventGroups = skill.ServerEvents.GroupBy(s => s.GetType()).ToDictionary(g => g.Key.Name, g => g.ToList());

        
        float currentY = 0;
        float width = rect.width;

        // --- 绘制辅助线：技能结束线 (Duration Line) ---
        float endLineX = skill.Duration * (PIXELS_PER_SECOND * timelineZoom);
        EditorGUI.DrawRect(new Rect(endLineX, 0, 2, rect.height), new Color(1f, 0.2f, 0.2f, 0.8f)); // 红色结束线

        // --- 辅助方法：绘制分组 ---
        void DrawGroup<T>(string title, Dictionary<string, List<T>> groups, bool isPhase)
        {
            if (!groups.Any()) return;
            
            GUI.Label(new Rect(5, currentY, 200, 20), title, EditorStyles.boldLabel);
            currentY += 20;

            foreach (var group in groups)
            {
                Rect trackRect = new Rect(0, currentY, width, TRACK_HEIGHT);
                EditorGUI.DrawRect(trackRect, new Color(0.25f, 0.25f, 0.25f, 0.3f));
                GUI.Label(new Rect(5, currentY + 5, 150, 20), group.Key, EditorStyles.miniLabel);
                foreach (var item in group.Value)
                {
                    if (isPhase)
                    {
                        DrawPhaseItem(item as SkillPhase, trackRect);
                    }
                    else
                    {
                        DrawEventItem(item as SkillEvent, trackRect);
                    }
                }

                currentY += TRACK_HEIGHT + TRACK_SPACING;
            }
            currentY += 10;
        }

        DrawGroup("Client Phases", clientPhaseGroups, true);
        DrawGroup("Client Events", clientEventGroups, false);
        
        // 分割线
        EditorGUI.DrawRect(new Rect(0, currentY - 5, width, 2), new Color(0.1f, 0.1f, 0.1f));
        
        DrawGroup("Server Phases", serverPhaseGroups, true);
        DrawGroup("Server Events", serverEventGroups, false);
    }
    
    void DrawPhaseItem(SkillPhase phase, Rect trackRect)
    {
        float pixelsPerSecond = PIXELS_PER_SECOND * timelineZoom;
        float startX = phase.StartTime * pixelsPerSecond;
        float durationWidth = (phase.EndTime - phase.StartTime) * pixelsPerSecond;
        durationWidth = Mathf.Max(durationWidth, 2f);

        Rect phaseRect = new Rect(startX, trackRect.y + 4, durationWidth, TRACK_HEIGHT - 8);
        
        Color baseColor = (activeItem == phase) ? new Color(0.3f, 0.6f, 0.9f) : new Color(0.3f, 0.5f, 0.7f);
        EditorGUI.DrawRect(phaseRect, baseColor);
        Handles.color = new Color(0.7f, 0.7f, 0.7f);
        Handles.DrawWireCube(phaseRect.center, phaseRect.size);


        if (durationWidth > 40)
        {
            GUI.Label(phaseRect, phase.GetType().FullName, EditorStyles.whiteMiniLabel);
        }
        
        Rect leftHandle = new Rect(phaseRect.x, phaseRect.y, HANDLE_WIDTH, phaseRect.height);
        EditorGUIUtility.AddCursorRect(leftHandle, MouseCursor.ResizeHorizontal);
        
        Rect rightHandle = new Rect(phaseRect.xMax - HANDLE_WIDTH, phaseRect.y, HANDLE_WIDTH, phaseRect.height);
        EditorGUIUtility.AddCursorRect(rightHandle, MouseCursor.ResizeHorizontal);
        Rect moveHandle = new Rect(phaseRect.x + HANDLE_WIDTH, phaseRect.y, Mathf.Max(0, phaseRect.width - HANDLE_WIDTH * 2), phaseRect.height);
        EditorGUIUtility.AddCursorRect(moveHandle, MouseCursor.MoveArrow);
        
        ProcessPhaseEvents(phase, leftHandle, rightHandle, moveHandle);
    }
    
    void DrawEventItem(SkillEvent evt, Rect trackRect)
    {
        float pixelsPerSecond = PIXELS_PER_SECOND * timelineZoom;
        float x = evt.Time * pixelsPerSecond;
        
        Rect iconRect = new Rect(x - 5, trackRect.y + 5, 10, 20);

        Color color = (activeItem == evt) ? Color.yellow : Color.white;
        
        EditorGUI.DrawRect(iconRect, color);
        
        // Tooltip
        if (iconRect.Contains(Event.current.mousePosition))
        {
            GUI.Label(new Rect(x + 10, trackRect.y, 100, 20), evt.GetType().FullName);
        }

        EditorGUIUtility.AddCursorRect(iconRect, MouseCursor.MoveArrow);

        ProcessEventEvents(evt, iconRect);
    }
    
    
    void ProcessPhaseEvents(SkillPhase phase, Rect left, Rect right, Rect center)
    {
        Event e = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    if (left.Contains(e.mousePosition))
                    {
                        StartDrag(DragMode.ResizeStart, phase);
                        e.Use();
                    }
                    else if (right.Contains(e.mousePosition))
                    {
                        StartDrag(DragMode.ResizeEnd, phase);
                        e.Use();
                    }
                    else if (center.Contains(e.mousePosition))
                    {
                        StartDrag(DragMode.MovePhase, phase);
                        e.Use();
                    }
                }
                break;
        }
    }

    void ProcessEventEvents(SkillEvent evt, Rect iconRect)
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && iconRect.Contains(e.mousePosition))
        {
            StartDrag(DragMode.MoveEvent, evt);
            e.Use();
        }
    }

    void StartDrag(DragMode mode, object item)
    {
        currentDragMode = mode;
        activeItem = item;
        float pixelsPerSecond = PIXELS_PER_SECOND * timelineZoom;
        
        float mouseTime = Event.current.mousePosition.x / pixelsPerSecond;
        if (item is SkillPhase p)
        {
            // 记录原始数据
            dragItemStartTime = p.StartTime;
            dragItemDuration = p.EndTime - p.StartTime;
            dragTimeOffset = mouseTime - p.StartTime; 
        }
        else if (item is SkillEvent evt)
        {
            dragItemStartTime = evt.Time;
            dragTimeOffset = mouseTime - evt.Time;
        }
        
        GUI.FocusControl(null);
    }
    
    void HandleDragLogic()
    {
        if (currentDragMode == DragMode.None || activeItem == null) return;

        Event e = Event.current;
        
        if (e.type == EventType.MouseUp)
        {
            currentDragMode = DragMode.None;
            Repaint();
            return;
        }

        if (e.type == EventType.MouseDrag)
        {
            float pixelsPerSecond = PIXELS_PER_SECOND * timelineZoom;
            
            float currentMouseTime = e.mousePosition.x / pixelsPerSecond;

            if (activeItem is SkillPhase phase)
            {
                if (currentDragMode == DragMode.MovePhase)
                {
                    float newStartTime = currentMouseTime - dragTimeOffset;
                    phase.StartTime = Mathf.Max(0, newStartTime);
                    phase.EndTime = phase.StartTime + dragItemDuration;
                }
                else if (currentDragMode == DragMode.ResizeStart)
                {
                    float newStart = Mathf.Max(0, currentMouseTime); 
                    phase.StartTime = Mathf.Min(newStart, phase.EndTime - 0.05f);
                }
                else if (currentDragMode == DragMode.ResizeEnd)
                {
                    // Resize End：鼠标在哪里，右边缘就在哪里
                    float newEnd = Mathf.Max(0, currentMouseTime);
                    // 限制：不能小于开始时间
                    phase.EndTime = Mathf.Max(phase.StartTime + 0.05f, newEnd);
                }
            }
            else if (activeItem is SkillEvent evt && currentDragMode == DragMode.MoveEvent)
            {
                float newTime = currentMouseTime - dragTimeOffset;
                evt.Time = Mathf.Max(0, newTime);
            }

            e.Use();
            Repaint();
        }
    }
    
    
    void DeleteActiveItem()
    {
        if (activeItem == null) return;
        if (selectedSkillIndex < 0 || selectedSkillIndex >= skillConfigs.Count) return;
        
        var skill = skillConfigs[selectedSkillIndex];
        bool found = false;
        
        
        if (activeItem is SkillPhase phase)
        {
            if (skill.ClientPhases.Remove(phase)) found = true;
            else if (skill.ServerPhases.Remove(phase)) found = true;
        }
        else if (activeItem is SkillEvent evt)
        {
            if (skill.ClientEvents.Remove(evt)) found = true;
            else if (skill.ServerEvents.Remove(evt)) found = true;
        }

        if (found)
        {
            activeItem = null;
            Debug.Log("Item deleted.");
            Repaint();
        }
        else
        {
            Debug.LogWarning("Failed to delete item: not found in current skill configuration.");
        }
    }
    
    
    
   private AnimationClip tempBakeClip; 
    void DrawInspectorArea()
    {
        GUILayout.Label("Inspector", EditorStyles.boldLabel);
        GUILayout.Space(10);
        if (activeItem == null)
        {
            EditorGUILayout.HelpBox("Select an item in the timeline to edit properties.", MessageType.Info);
            return;
        }
        inspectorScrollPos = GUILayout.BeginScrollView(inspectorScrollPos);
        EditorGUI.BeginChangeCheck();

        // --- 修复开始：使用完整的 if - else if - else 链 ---
        if (activeItem is AnimationEvent animEvent)
        {
            DrawAnimationEventInspector(animEvent);
        }
        else if (activeItem is MoveStepPhase moveStepPhase)
        {
            DrawMoveStepPhaseInspector(moveStepPhase);
        }
        else if (activeItem is EffectEvent vfxEvent)
        {
            DrawVFXEventInspector(vfxEvent);
        }
        else
        {
            DrawReflectionInspector(activeItem);
        }
        
        if (EditorGUI.EndChangeCheck())
        {
            Repaint(); 
        }
        
        GUILayout.Space(10);
        
        GUILayout.BeginHorizontal();
        Color oldColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f); 
        if (GUILayout.Button("Delete Selected Item", GUILayout.Width(150)))
        {
            DeleteActiveItem();
        }
        GUI.backgroundColor = oldColor;
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
    }

    void DrawMoveStepPhaseInspector(MoveStepPhase movePhase)
    {
        EditorGUILayout.LabelField("Root Motion Tools", EditorStyles.boldLabel);
        GUILayout.BeginVertical("box");
        
        tempBakeClip = (AnimationClip)EditorGUILayout.ObjectField("Source Clip", tempBakeClip, typeof(AnimationClip), false, GUILayout.Width(300));

        if (GUILayout.Button("Auto Bake Data"))
        {
            if (tempBakeClip == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an Animation Clip first.", "OK");
            }
            else
            {
                var result = RootMotionAnalyzer.Bake(previewObject, tempBakeClip, 60, 0.02f);
                
                Undo.RecordObject(this, "Bake Root Motion");
                movePhase.StartTime = result.MoveStartTime;
                movePhase.EndTime = result.MoveEndTime;
                
                movePhase.Distance = result.TotalDistance;
                movePhase.Curve = result.MotionCurve;

                Debug.Log($"Bake Applied: Distance {result.TotalDistance:F2}, Duration {result.MoveEndTime - result.MoveStartTime:F2}s");
                
                // 强制刷新界面
                GUI.FocusControl(null);
                Repaint();
            }
        }
        GUILayout.EndVertical();
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Phase Properties", EditorStyles.boldLabel);
        DrawReflectionInspector(movePhase);
    }


    void DrawAnimationEventInspector(AnimationEvent animEvt)
    {
        EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
        
        AnimationClip clip = null;
        if (!string.IsNullOrEmpty(animEvt.Animation))
        {
            string path = $"{ANIMATION_ROOT_PATH}{animEvt.Animation}.anim"; 
            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        // 修改：使用水平布局，将标签和字段分开
        AnimationClip newClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", clip, typeof(AnimationClip), false, GUILayout.Width(300));

        // 如果用户拖入了新的 Clip
        if (newClip != clip)
        {
            if (newClip != null)
            {
                animEvt.Animation = newClip.name;
                Debug.Log($"Auto set duration from clip: {newClip.length}");
            }
            else
            {
                animEvt.Animation = "";
            }
        }

        // 显示动画的只读信息 (辅助信息)
        if (newClip != null)
        {
            EditorGUILayout.HelpBox($"Duration: {newClip.length:F3}s\nFrameRate: {newClip.frameRate}", MessageType.None);
        }
        
        GUILayout.Space(10);
        GUILayout.Label("General Properties", EditorStyles.boldLabel);
        DrawReflectionInspector(animEvt, new List<string> { "Animation" });
    }

    // --- 特殊处理：特效事件 ---
    void DrawVFXEventInspector(EffectEvent vfxEvt)
    {
        EditorGUILayout.LabelField("VFX Settings", EditorStyles.boldLabel);
        
        GameObject prefab = null;
        if (!string.IsNullOrEmpty(vfxEvt.Effect))
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EFFECT_ROOT_PATH + vfxEvt.Effect + ".prefab");
        }
        
        GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField("VFX Prefab", prefab, typeof(GameObject), false, GUILayout.Width(300));

        if (newPrefab != prefab)
        {
            if (newPrefab != null)
            {
                // 获取 AssetPath
                vfxEvt.Effect = newPrefab.name;
            }
            else
            {
                vfxEvt.Effect = "";
            }
        }
        
        DrawReflectionInspector(vfxEvt, new List<string> { "Effect", "Type", "Time" });
    }

   void DrawReflectionInspector(object target, List<string> ignoredFields = null)
    {
        var properties = target.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (ignoredFields != null && ignoredFields.Contains(prop.Name)) continue;
            
            if (!prop.CanRead) continue;

            object value = prop.GetValue(target);
            object newValue = null;
            
            if (prop.Name == "Type") continue;
            
            // --- 修改开始：移除 BeginHorizontal 和 GUILayout.Width，使用标准带 Label 的 API ---
            
            if (prop.PropertyType == typeof(int))
            {
                newValue = EditorGUILayout.IntField(prop.Name, (int)value, GUILayout.Width(300));
            }
            else if (prop.PropertyType == typeof(float))
            {
                newValue = EditorGUILayout.FloatField(prop.Name, (float)value, GUILayout.Width(300));
            }
            else if (prop.PropertyType == typeof(string))
            {
                newValue = EditorGUILayout.TextField(prop.Name, (string)value, GUILayout.Width(300));
            }
            else if (prop.PropertyType == typeof(bool))
            {
                newValue = EditorGUILayout.Toggle(prop.Name, (bool)value);
            }
            else if (prop.PropertyType == typeof(Vector3))
            {
                bool originalWideMode = EditorGUIUtility.wideMode;
                EditorGUIUtility.wideMode = false;
                
                newValue = EditorGUILayout.Vector3Field(prop.Name, (Vector3)value, GUILayout.Width(300));
                EditorGUIUtility.wideMode = originalWideMode;
            }
            else if (prop.PropertyType == typeof(Quaternion))
            {

                var oldValue = (Quaternion)value;
                var v = EditorGUILayout.Vector3Field(prop.Name, oldValue.eulerAngles, GUILayout.Width(300));
                if(v != oldValue.eulerAngles) newValue = Quaternion.Euler(v);
            }
            else if (prop.PropertyType == typeof(Color))
            {
                newValue = EditorGUILayout.ColorField(prop.Name, (Color)value, GUILayout.Width(300));
            }
            else if (prop.PropertyType.IsEnum)
            {
                newValue = EditorGUILayout.EnumPopup(prop.Name, (Enum)value, GUILayout.Width(300));
            }
            
            // --- 修改结束 ---

            if (prop.CanWrite && newValue != null && !newValue.Equals(value))
            {
                if (target is SkillPhase phase)
                {
                    if (prop.Name == "StartTime")
                    {
                        float newStart = (float)newValue;
                        newValue = Mathf.Max(0, newStart);
                    }
                    else if (prop.Name == "EndTime")
                    {
                        float newEnd = (float)newValue;
                        newValue = Mathf.Max(phase.StartTime, newEnd);
                    }
                }

                prop.SetValue(target, newValue);
            }
        }
    }


    private void LoadJsonFile()
    {     
        try
        {
            if (File.Exists(jsonFilePath))
            {
                SkillTimelineJsonSerializer.Deserializer(jsonFilePath);
                skillConfigs = SkillTimelineJsonSerializer.SkillConfigs.Values.OrderBy(s => s.Id).ToList();
                skillOptions = new string[skillConfigs.Count];
                for (int i = 0; i < skillConfigs.Count; i++)
                {
                    skillOptions[i] = $"{skillConfigs[i].Id}:{skillConfigs[i].Name}";
                }

                selectedSkillIndex = 0;
                isDataLoaded = true;

                Debug.Log($"Successfully loaded {skillConfigs.Count} skill configurations.");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "File not found: " + jsonFilePath, "OK");
                isDataLoaded = false;
            }
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("Error", "Failed to load JSON file: " + ex.Message, "OK");
            isDataLoaded = false;
        }
    }


    private void SaveToFile()
    {
        if (!isDataLoaded || skillConfigs == null)
        {
            EditorUtility.DisplayDialog("Error", "No data to save. Please load a JSON file first.", "OK");
            return;
        }

        try
        {
            SkillTimelineJsonSerializer.Serializer(skillConfigs, jsonFilePath);
            EditorUtility.DisplayDialog("Success", "File saved successfully!", "OK");
            Debug.Log("Skill timeline configurations saved to: " + jsonFilePath);
        }        
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("Error", "Failed to save file: " + ex.Message, "OK");
        }
    }

    private SkillEvent CreateEventInstance(string type)
    {
        switch (type)
        {
            case nameof(AnimationEvent): return new AnimationEvent();
            case nameof(EffectEvent): return new EffectEvent();
            default: throw new System.Exception("Unknown Event type: " + type);
        }
    }

    private SkillPhase CreatePhaseInstance(string type)
    {
        switch (type)
        {
            case nameof(OpenComboWindowPhase): return new OpenComboWindowPhase();
            case nameof(MoveStepPhase): return new MoveStepPhase();
            default: throw new System.Exception("Unknown Phase type: " + type);
        }
    }

}
#endif
