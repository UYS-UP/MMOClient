using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 血条视图
/// 显示实体头顶的血条,跟随目标移动
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public Image healthImage;
    public TMP_Text healthText;
    public CanvasGroup canvasGroup;

    private Transform target;
    private Camera worldCamera;
    private Camera uiCamera;
    private RectTransform rectTransform;
    private RectTransform canvasRect;
    private readonly Vector3 offset = Vector3.up * 2.2f;
    private Vector2 anchoredPos;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasRect = UIService.Instance.ScreenCanvasRect;
        uiCamera =  Camera.main;
        anchoredPos = Vector2.zero;
    }

    /// <summary>
    /// 设置跟随目标
    /// </summary>
    public void SetTarget(Transform targetTransform, Camera camera)
    {
        target = targetTransform;
        worldCamera = camera;
    }

    /// <summary>
    /// 更新血条显示
    /// </summary>
    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        healthText.text = currentHealth + "/" + maxHealth;
        float healthPercent = (float)currentHealth / (float)maxHealth;
        healthImage.DOFillAmount(healthPercent, 0.2f);
        
    }

    private void LateUpdate()
    {
        if (target == null || worldCamera == null || target == null)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        Vector3 worldPos = target.position + offset;
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0f || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)
        {
            canvasGroup.alpha = 0f;
            return;
        }
        
        Vector2 viewportPos = worldCamera.WorldToViewportPoint(worldPos);
        anchoredPos.x = (viewportPos.x - 0.5f) * canvasRect.sizeDelta.x;
        anchoredPos.y = (viewportPos.y - 0.5f) * canvasRect.sizeDelta.y;

        rectTransform.anchoredPosition = anchoredPos;
        canvasGroup.alpha = 1f;
    }
}