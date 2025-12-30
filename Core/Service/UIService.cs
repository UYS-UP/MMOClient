using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public enum UILayer
{
    Scene = 0,           // 场景UI：血条、名字、伤害数字等
    Background = 100,    // 背景界面：主界面底栏、小地图背景
    Normal = 200,        // 普通界面：功能面板、背包、技能栏
    Popup = 300,         // 弹出窗口：设置、商城、任务详情
    Alert = 400,         // 警告框：确认对话框、重要提示
    Guide = 500,         // 引导层：新手引导、功能引导
    Toast = 600,         // 提示信息：系统消息、获得物品
    Loading = 700,       // 加载界面：场景切换加载
    System = 800,        // 系统级：调试信息、GM命令
}

public class UIService : SingletonMono<UIService>
{
    private readonly Dictionary<string, BaseView> panelDict = new Dictionary<string, BaseView>();
    private readonly Dictionary<UILayer, RectTransform> layerContainers = new Dictionary<UILayer, RectTransform>();
    
    public Canvas ScreenCanvas { get; private set; }
    public RectTransform ScreenCanvasRect { get; private set; }
    
    public Canvas WorldCanvas { get; private set; }
    public RectTransform WorldCanvasRect { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        InitializeCanvas("Prefabs/UI/", "ScreenCanvas", "WorldCanvas");
        InitializeEventSystem("Prefabs/UI/EventSystem");
    }

    private void Start()
    {
        ScreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
       
        WorldCanvas.renderMode = RenderMode.WorldSpace;
        WorldCanvas.worldCamera = GameContext.Instance.MainCamera;
    }

    private void InitializeCanvas(string path, string screenCanvasName, string worldCanvasName)
    {
        GameObject screenCanvas = Instantiate(Resources.Load<GameObject>(path + screenCanvasName));
        GameObject worldCanvas = Instantiate(Resources.Load<GameObject>(path + worldCanvasName));
        ScreenCanvasRect = screenCanvas.transform.GetComponent<RectTransform>();
        ScreenCanvas = screenCanvas.GetComponent<Canvas>();
        WorldCanvasRect = worldCanvas.transform.GetComponent<RectTransform>();
        WorldCanvas = worldCanvas.GetComponent<Canvas>();
        DontDestroyOnLoad(screenCanvas);
        DontDestroyOnLoad(worldCanvas);
        InitializeLayerContainers();
    }

    private void InitializeEventSystem(string path)
    {
        GameObject eventSystemObj = Instantiate(Resources.Load<GameObject>(path));
        DontDestroyOnLoad(eventSystemObj);
    }
    
    private void InitializeLayerContainers()
    {
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            GameObject layerObj = new GameObject(layer + "Layer");
            RectTransform layerRect = layerObj.AddComponent<RectTransform>();
            layerRect.SetParent(ScreenCanvasRect, false);
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            layerContainers[layer] = layerRect;

            // 设置层级顺序
            Canvas layerCanvas = layerObj.AddComponent<Canvas>();
            layerCanvas.overrideSorting = true;
            layerCanvas.sortingOrder = (int)layer;
        }
    }

    /// <summary>
    /// 显示面板
    /// </summary>
    /// <param name="onBegin">开始回调</param>
    /// <param name="onComplete">结束回调</param>
    /// <param name="fadeOut">是否开启淡出效果</param>
    /// <param name="layer">面板层级</param>
    /// <param name="worldSpace">是否使用世界空间</param>
    public T ShowView<T>(Action<T> onBegin = null, Action<T> onComplete = null,
        bool fadeOut = false, UILayer layer = UILayer.Normal, bool worldSpace = false) where T : BaseView
    {
        string panelName = typeof(T).ToString();
        if (!panelDict.ContainsKey(panelName))
        {
            AddPanel<T>(layer, worldSpace);
        }
        var panel = GetView<T>();
        if (panel == null) return null;
        if(panel.isVisible) return panel;
        panel.ShowMe(onBegin, onComplete, fadeOut);
        return panel;
    }

    /// <summary>
    /// 显示面板
    /// </summary>
    /// <param name="onBegin">开始回调</param>
    /// <param name="onComplete">结束回调</param>
    /// <param name="fadeIn">是否开启淡入效果</param>
    public void HidePanel<T>(Action<T> onBegin = null, Action<T> onComplete = null,
        bool fadeIn = false) where T : BaseView
    {
        if (!panelDict.TryGetValue(typeof(T).ToString(), out var panel)) return;
        if(!panel.isVisible) return;
        panel.HideMe(onBegin, onComplete, fadeIn);
    }

    /// <summary>
    /// 添加面板
    /// </summary>
    /// <param name="layer">面板层级</param>
    /// <param name="worldSpace">是否使用世界空间</param>
    public void AddPanel<T>(UILayer layer = UILayer.Normal, bool worldSpace = false)  where T : BaseView
    {
        string panelName = typeof(T).ToString();
        if (panelDict.ContainsKey(panelName)) return;
        var parent = worldSpace ?  WorldCanvasRect : layerContainers[layer];
        GameObject panelObj = Object.Instantiate(
            Resources.Load<GameObject>($"Prefabs/UI/Views/{panelName}"), 
            parent, 
            false
        );
        BaseView view = panelObj.GetComponent<BaseView>();
        panelDict[panelName] = view;
        view.canvasGroup.alpha = 0;
        ChangeLayer<T>(layer);
    }

    public T GetView<T>(UILayer layer = UILayer.Normal) where T : BaseView
    {
        string panelName = typeof(T).ToString();
        if (panelDict.TryGetValue(panelName, out var panel))
        {
            return panel as T;
        }
        return null;
    }

    public bool PanelIsVisible<T>(UILayer layer = UILayer.Normal)
    {
        string panelName = typeof(T).ToString();
        return panelDict.ContainsKey(panelName);
    }
    
    /// <summary>
    /// 删除面板
    /// </summary>
    public void RemovePanel<T>() where T : BaseView
    {
        string panelName = typeof(T).ToString();
        if(!panelDict.ContainsKey(panelName)) return;
        Destroy(panelDict[panelName].gameObject);
        panelDict.Remove(panelName);
        
    }

    /// <summary>
    /// 移除所有面板
    /// </summary>
    public void RemoveAllPanel()
    {
        foreach (var panel in panelDict.Values)
        {
            Destroy(panel.gameObject);
        }
        panelDict.Clear();
    }

    /// <summary>
    /// 改变渲染层级
    /// </summary>
    /// <param name="newLayer">层级</param>
    /// <param name="setAsFristSibling">设置成第一个</param>
    /// <param name="setAsLastSibling">设置成最后一个</param>
    public void ChangeLayer<T>(UILayer newLayer, bool setAsLastSibling = false, bool setAsFristSibling = false)
    {
        string panelName = typeof(T).ToString();
        if(!panelDict.TryGetValue(panelName, out var panel)) return;
        if(!layerContainers.TryGetValue(newLayer, out var newParent)) return;
        
        panel.UpdateLayer(newLayer, newParent);
        if(setAsLastSibling) panel.transform.SetAsLastSibling();
        if(setAsFristSibling) panel.transform.SetAsFirstSibling();
    }
    
    
    
}
