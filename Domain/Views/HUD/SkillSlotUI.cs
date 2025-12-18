using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image IconImage;
    public Image MaskImage;
    public TextMeshProUGUI CooldownText;
    private float BaseCooldown;
    private Tween cooldownTween;
    private float currentCooldown;
    private int SkillId;

    public void Initialize(Sprite icon, float baseCooldown, int skillId)
    {
        SkillId = skillId;
        IconImage.sprite = icon;
        BaseCooldown = baseCooldown;
        ResetCooldown(); // 初始化时重置冷却状态
    }

    public void StartCooldown()
    {
        // 停止之前的动画
        cooldownTween?.Kill();
        
        // 重置状态
        MaskImage.fillAmount = 1f;
        currentCooldown = BaseCooldown;
        CooldownText.text = $"{BaseCooldown:F1}";
        CooldownText.gameObject.SetActive(true);
        
        // 开始冷却动画
        cooldownTween = DOTween.To(
            () => currentCooldown, 
            value => {
                currentCooldown = value;
                MaskImage.fillAmount = value / BaseCooldown;
                CooldownText.text = $"{value:F1}";
            }, 
            0f, 
            BaseCooldown
        ).SetEase(Ease.Linear).OnComplete(OnCooldownComplete);
    }
    
    private void OnCooldownComplete()
    {
        CooldownText.gameObject.SetActive(false);
        MaskImage.fillAmount = 0f;
    }
    
    public void ResetCooldown()
    {
        cooldownTween?.Kill();
        MaskImage.fillAmount = 0f;
        CooldownText.gameObject.SetActive(false);
        currentCooldown = 0f;
    }
    
    private void OnDestroy()
    {
        cooldownTween?.Kill();
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        UIService.Instance.GetView<GameView>().ShowSkillInfo("普通攻击");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIService.Instance.GetView<GameView>().HideSkillInfo();
    }
}
