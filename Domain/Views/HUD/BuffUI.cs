
using System;
using DG.Tweening;
using UnityEngine;

public class BuffUI : MonoBehaviour
{
    
    private Tween cooldownTween;
    private float baseDuration;
    private float duration;

    
    private UnityEngine.UI.Image IconImage;
    private UnityEngine.UI.Image MaskImage;
    private TMPro.TextMeshProUGUI DurationText;

    private void BindComponent()
    {
        var root = this.transform;

        IconImage = root.Find("IconImage")?.GetComponent<UnityEngine.UI.Image>();

        MaskImage = root.Find("IconImage/MaskImage")?.GetComponent<UnityEngine.UI.Image>();

        DurationText = root.Find("DurationText")?.GetComponent<TMPro.TextMeshProUGUI>();

    }


    private void Awake()
    {
        BindComponent();
    }

    public void Initialize(int buffId, float baseDuration)
    {
        this.baseDuration = baseDuration;
        StartCooldown();
    }
    
    
    public void StartCooldown()
    {
        cooldownTween?.Kill();
        
        MaskImage.fillAmount = 1f;
        duration = baseDuration;
        DurationText.text = $"{baseDuration:F1}";
        DurationText.gameObject.SetActive(true);

        cooldownTween = DOTween.To(
            () => duration, 
            value => {
                duration = value;
                MaskImage.fillAmount = value / baseDuration;
                DurationText.text = $"{value:F1}";
            }, 
            0f, 
            baseDuration
        ).SetEase(Ease.Linear).OnComplete(OnBuffComplete);
    }
    
    private void OnBuffComplete()
    {
        DurationText.gameObject.SetActive(false);
        MaskImage.fillAmount = 0f;
        PoolService.Instance.Despawn(this.gameObject);
    }
    
    
    private void OnDestroy()
    {
        cooldownTween?.Kill();
    }
}
