
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameSceneView : BaseView
{
    private readonly Dictionary<int, HealthBarUI> healthBars = new Dictionary<int, HealthBarUI>();
    
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
    private GameObject damagePrefab;

    protected override void Awake()
    {
        base.Awake();
        BindComponent();
        controller = new GameSceneController(this);
        DialogueTipRect.gameObject.SetActive(false);
        damagePrefab = Resources.Load<GameObject>("Prefabs/UI/HUD/DamageUI");
        PoolService.Instance.Preload(damagePrefab, 20);
    }

    public void Update()
    {
        controller.Update();
    }

    public void LoadRegionScene(int mapId)
    {
        SceneService.Instance.LoadThenInvoke($"GameScene_{mapId}", () =>
        {
            Instantiate(ResourceService.Instance.LoadResource<GameObject>("Prefabs/EntityWorld"));
            GameClient.Instance.Send(Protocol.CS_EnterRegion, new ClientEnterRegion {RegionId = mapId});
        });
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

    
    public void UpdateHealthBar(int entityId, float currentHp, float maxHp)
    {
        if (!healthBars.TryGetValue(entityId, out var healthBar)) return;
        healthBar.UpdateHealthBar(currentHp, maxHp);
        if (currentHp != 0) return;
        Destroy(healthBar.gameObject);
        healthBars.Remove(entityId);
    }
    
    public void CreateHealthBar(int entityId, Transform target, float currentHp, float maxHp)
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
    

    public void DestroyHealthBar(int entityId)
    {
        if (healthBars.TryGetValue(entityId, out var healthBar))
        {
            Destroy(healthBar.gameObject);
            healthBars.Remove(entityId);
        }
    }

    public void CreateDamageText(Vector3 pos, float damage)
    {
        var damageUI = PoolService.Instance.Spawn(damagePrefab, DamageContainerRect).GetComponent<DamageUI>();
        damageUI.Initialize(damage, pos, Color.red);
    }
    
}
