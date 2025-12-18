using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneService : SingletonMono<SceneService>
{
    [Header("Optional Fade")]
    [SerializeField] private float defaultFadeDuration = 0.25f;

    private CanvasGroup fadeCg;
    private bool isFading;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        EnsureFadeCanvas();
    }

    // ========== 基础查询 ==========
    public string ActiveSceneName => SceneManager.GetActiveScene().name;
    public int ActiveSceneBuildIndex => SceneManager.GetActiveScene().buildIndex;

    // ========== 常驻对象 ==========
    public void MakePersistent(GameObject go)
    {
        if (go != null) DontDestroyOnLoad(go);
    }

    // ========== 预加载（不激活） ==========
    /// <summary>
    /// 预加载场景但不激活（进入“就绪”状态）。返回 AsyncOperation，稍后手动激活。
    /// </summary>
    public AsyncOperation PreloadScene(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (op == null) return null;
        op.allowSceneActivation = false;
        return op;
    }

    /// <summary>
    /// 激活通过 PreloadScene 得到的 AsyncOperation（切换到该场景）。
    /// </summary>
    public IEnumerator ActivatePreloaded(AsyncOperation preloadOp, bool setActive = true, bool waitOneFrameAfterLoaded = true)
    {
        if (preloadOp == null) yield break;
        preloadOp.allowSceneActivation = true;
        while (!preloadOp.isDone) yield return null;

        var target = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
        if (setActive && target.IsValid()) SceneManager.SetActiveScene(target);

        if (waitOneFrameAfterLoaded) yield return null; // 等一帧，让 OnEnable/Start 都执行
    }

    // ========== 异步加载（Single） ==========
    /// <summary>
    /// 加载场景（Single），完成后回调。可选淡入淡出、可选多等一帧。
    /// </summary>
    public void LoadSceneAsync(
        string sceneName,
        Action onLoaded = null,
        bool waitOneFrameAfterLoaded = true,
        bool withFade = false,
        float fadeDuration = -1f)
    {
        StartCoroutine(CoLoadSceneAsync(sceneName, onLoaded, waitOneFrameAfterLoaded, withFade, fadeDuration));
    }

    private IEnumerator CoLoadSceneAsync(string sceneName, Action onLoaded, bool waitOneFrame, bool withFade, float fadeDuration)
    {
        if (withFade) yield return CoFade(1f, fadeDuration);

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (op != null && !op.isDone) yield return null;

        if (waitOneFrame) yield return null;

        onLoaded?.Invoke();

        if (withFade) yield return CoFade(0f, fadeDuration);
    }

    // ========== 异步加载（Additive） ==========
    /// <summary>
    /// 加载场景（Additive），完成后可设为 Active，再回调。
    /// </summary>
    public void LoadSceneAdditiveAsync(
        string sceneName,
        bool setActiveAfterLoad = true,
        Action onLoaded = null,
        bool waitOneFrameAfterLoaded = true)
    {
        StartCoroutine(CoLoadSceneAdditive(sceneName, setActiveAfterLoad, onLoaded, waitOneFrameAfterLoaded));
    }

    private IEnumerator CoLoadSceneAdditive(string sceneName, bool setActive, Action onLoaded, bool waitOneFrame)
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        var scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid() && setActive) SceneManager.SetActiveScene(scene);

        if (waitOneFrame) yield return null;
        onLoaded?.Invoke();
    }

    // ========== 卸载 / 重载 ==========
    public void UnloadSceneAsync(string sceneName, Action onUnloaded = null)
    {
        StartCoroutine(CoUnload(sceneName, onUnloaded));
    }

    private IEnumerator CoUnload(string sceneName, Action onUnloaded)
    {
        if (!SceneManager.GetSceneByName(sceneName).IsValid())
        {
            onUnloaded?.Invoke();
            yield break;
        }
        var op = SceneManager.UnloadSceneAsync(sceneName);
        while (op != null && !op.isDone) yield return null;
        onUnloaded?.Invoke();
    }

    public void ReloadActiveScene(Action onReloaded = null, bool waitOneFrameAfterLoaded = true)
    {
        LoadSceneAsync(ActiveSceneName, onReloaded, waitOneFrameAfterLoaded);
    }
    
    public void LoadThenInvoke(string sceneName, Action actionAfterSceneReady, bool waitOneFrameAfterLoaded = true, bool withFade = false)
    {
        LoadSceneAsync(
            sceneName,
            onLoaded: () => { actionAfterSceneReady?.Invoke(); },
            waitOneFrameAfterLoaded: waitOneFrameAfterLoaded,
            withFade: withFade);
    }
    
    private void EnsureFadeCanvas()
    {
        if (fadeCg != null) return;

        var go = new GameObject("[SceneService-FadeCanvas]");
        MakePersistent(go);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var raycaster = go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var imageGo = new GameObject("Fade");
        imageGo.transform.SetParent(go.transform, false);

        var rect = imageGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var img = imageGo.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;

        fadeCg = imageGo.AddComponent<CanvasGroup>();
        fadeCg.alpha = 0f;
        fadeCg.blocksRaycasts = false;
        fadeCg.interactable = false;
    }

    private IEnumerator CoFade(float target, float duration)
    {
        if (duration < 0f) duration = defaultFadeDuration;
        isFading = true;
        EnsureFadeCanvas();

        float start = fadeCg.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
            fadeCg.alpha = a;
            fadeCg.blocksRaycasts = a > 0.001f; // 淡入时阻塞点击
            yield return null;
        }
        fadeCg.alpha = target;
        fadeCg.blocksRaycasts = target > 0.001f;
        isFading = false;
    }
}