
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameSceneView : BaseView
{
    private readonly Dictionary<string, HealthBarUI> healthBars = new Dictionary<string, HealthBarUI>();
    
    private UnityEngine.RectTransform HealthBarContainerRect;
    private UnityEngine.RectTransform EntityNameContainerRect;
    private UnityEngine.RectTransform DamageContainerRect;
    private UnityEngine.RectTransform TipContainerRect;
    private UnityEngine.RectTransform DialogueTipRect;
    private TMPro.TextMeshProUGUI DialogueTipText;

    private void BindComponent()
    {
        var root = this.transform;

        HealthBarContainerRect = root.Find("HealthBarContainer") as RectTransform;

        EntityNameContainerRect = root.Find("EntityNameContainer") as RectTransform;

        DamageContainerRect = root.Find("DamageContainer") as RectTransform;

        TipContainerRect = root.Find("TipContainer") as RectTransform;

        DialogueTipRect = root.Find("TipContainer/DialogueTip") as RectTransform;

        DialogueTipText = root.Find("TipContainer/DialogueTip/DialogueTipText")?.GetComponent<TMPro.TextMeshProUGUI>();

    }

    private GameSceneController controller;

    protected override void Awake()
    {
        base.Awake();
        BindComponent();
        controller = new GameSceneController(this);
        DialogueTipRect.gameObject.SetActive(false);
    }

    public void Update()
    {
        controller.Update();
    }


    public void ShowDialogueTip(string message)
    {
        DialogueTipText.text = message;
        DialogueTipRect.gameObject.SetActive(true);
    }
    
    public void HideDialogueTip()
    {
        DialogueTipRect.gameObject.SetActive(false);
    }

    
    public void UpdateHealthBar(string entityId, int currentHp, int maxHp)
    {
        if (!healthBars.TryGetValue(entityId, out var healthBar)) return;
        healthBar.UpdateHealthBar(currentHp, maxHp);
        if (currentHp != 0) return;
        Destroy(healthBar.gameObject);
        healthBars.Remove(entityId);
    }
    
    public void CreateHealthBar(string entityId, Transform target, int currentHp, int maxHp)
    {
        if (healthBars.ContainsKey(entityId)) return;

        var obj = Instantiate(
            ResourceService.Instance.LoadResource<GameObject>("Prefabs/UI/HealthBarUI"),
            HealthBarContainerRect,
            false);

        var healthBar = obj.GetComponent<HealthBarUI>();
        healthBars[entityId] = healthBar;
        healthBar.SetTarget(target, GameContext.Instance.MainCamera);
        healthBar.UpdateHealthBar(currentHp, maxHp);
    }
    

    public void DestroyHealthBar(string entityId)
    {
        if (healthBars.TryGetValue(entityId, out var healthBar))
        {
            Destroy(healthBar.gameObject);
            healthBars.Remove(entityId);
        }
    }
}
