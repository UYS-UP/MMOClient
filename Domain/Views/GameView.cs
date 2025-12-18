using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 游戏主视图
/// 负责显示游戏主界面的各种UI元素
/// </summary>
public class GameView : BaseView
{
    private static readonly int FillLevel = Shader.PropertyToID("_FillLevel");


    private UnityEngine.RectTransform SkillSlotsRect;
    private UnityEngine.UI.Slider ExSlider;
    private UnityEngine.RectTransform SkillInfoRect;
    private TMPro.TextMeshProUGUI DescriptionText;
    private UnityEngine.UI.Image MpLiquidImage;
    private UnityEngine.UI.Image HpLiquidImage;
    
    private ScrollRect activeScrollRect;
    private RectTransform activeContent;
    
    private ScrollRect completedScrollRect;
    private RectTransform completedContent;

    private RectTransform dungeonLimitRect;
    
    private ScrollRect voteScrollRect;
    private RectTransform voteContainer;
    private RectTransform voteContent;

    private void BindComponent()
    {
        var root = this.transform;

        SkillSlotsRect = root.Find("SkillSlots") as RectTransform;

        ExSlider = root.Find("ExSlider")?.GetComponent<UnityEngine.UI.Slider>();
        
        SkillInfoRect = root.Find("SkillInfo") as RectTransform;

        DescriptionText = root.Find("SkillInfo/DescriptionText")?.GetComponent<TMPro.TextMeshProUGUI>();

        MpLiquidImage = root.Find("Mp/MpLiquidImage")?.GetComponent<UnityEngine.UI.Image>();

        HpLiquidImage = root.Find("Hp/HpLiquidImage")?.GetComponent<UnityEngine.UI.Image>();

        activeScrollRect = root.Find("QuestContainer/Scroll View")?.GetComponent<ScrollRect>();
        activeContent = activeScrollRect?.content;
        
        completedScrollRect = root.Find("QuestContainer/CompletedScroll")?.GetComponent<ScrollRect>();
        completedContent = completedScrollRect?.content;
        
        dungeonLimitRect = root.Find("DungeonLimit")?.GetComponent<RectTransform>();
        voteContainer = root.Find("VoteContainer")?.GetComponent<RectTransform>(); 
        voteScrollRect = voteContainer?.Find("VoteScrollView")?.GetComponent<ScrollRect>();
        voteContent = voteScrollRect?.content;
    }

    
    public GameObject QuestSlotUI;
    public Toggle activeQuestToggle;
    public Toggle completedQuestToggle;
    
    private Dictionary<string, QuestSlotUI> activeQuests = new Dictionary<string, QuestSlotUI>();
    private Dictionary<string, QuestSlotUI> completedQuests = new Dictionary<string, QuestSlotUI>();
    
    public TextMeshProUGUI limitTimeText;
    private Dictionary<string, LootChoiceUI> lootChoices = new Dictionary<string, LootChoiceUI>();
    public GameObject LootChoiceUI;
    
    public void ShowLimitTime(float time)
    {
        DOTween.To(() => time, x => time = x, 0, time).SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                limitTimeText.text = FormatTime(time);
            }).OnComplete(() =>
            {
                limitTimeText.text = "00:00";
            });
        dungeonLimitRect.gameObject.SetActive(true);
        
        
    }
    
    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return $"{minutes:00}:{seconds:00}";
    }
    
    public void HideLimitTime()
    {
        dungeonLimitRect.gameObject.SetActive(false);
    }
    public void AddQuest(QuestNode node)
    {
        var quest = Instantiate(QuestSlotUI, activeContent, false).GetComponent<QuestSlotUI>();
        activeQuests[node.NodeId] = quest;
        quest.UpdateQuest(node);
    }

    public void UpdateQuest(string nodeId, int index, QuestObjective objective)
    {
        if(!activeQuests.TryGetValue(nodeId, out var quest)) return;
        quest.UpdateQuest(index, objective);
    }

    public void CompleteQuest(QuestNode node)
    {
        if (!activeQuests.TryGetValue(node.NodeId, out var quest)) return;
        quest.canvasGroup.DOFade(0, 0.25f).OnComplete(() =>
        {
            quest.transform.SetParent(completedContent, false);
            quest.canvasGroup.alpha = 1;
            completedQuests[node.NodeId] = quest;
        });
    }
    
    private SkillSlotUI[] skillSlots;
    private Material hpLiquidMaterial;
    private Material mpLiquidMaterial;

    private GameController controller;
    
    protected override void Awake()
    {
        base.Awake();
        BindComponent();
        controller = new GameController(this);
        hpLiquidMaterial = HpLiquidImage.material;
        mpLiquidMaterial  = MpLiquidImage.material;
        ExSlider.maxValue = 1f;
        
        if (SkillSlotsRect != null)
        {
            skillSlots = new SkillSlotUI[SkillSlotsRect.childCount];
            for (var i = 0; i < SkillSlotsRect.childCount; i++)
            {
                skillSlots[i] = SkillSlotsRect.GetChild(i).GetComponent<SkillSlotUI>();
            }
        }
        
        skillSlots[0].Initialize(ResourceService.Instance.LoadResource<Sprite>("Sprites/Skills/1"), 2, 0);
        
        PoolService.Instance.Preload(QuestSlotUI, 10);
        completedScrollRect.gameObject.SetActive(false);
        activeQuestToggle.onValueChanged.AddListener((on) =>
        {
            completedScrollRect.gameObject.SetActive(!on);
            activeScrollRect.gameObject.SetActive(on);

        });
        
        completedQuestToggle.onValueChanged.AddListener(on =>
        {
            activeScrollRect.gameObject.SetActive(!on);
            completedScrollRect.gameObject.SetActive(on);

        });
        
        dungeonLimitRect.gameObject.SetActive(false);
        voteContainer.gameObject.SetActive(false);
        ProtocolRegister.Instance.OnLevelRegionEvent += OnLevelRegionEvent;
        ProtocolRegister.Instance.OnLevelDungeonEvent += OnLevelDungeonEvent;
        ProtocolRegister.Instance.OnPlayerEnterDungeonEvent += OnPlayerEnterDungeonEvent; 
        GameClient.Instance.RegisterHandler(Protocol.DungeonLootInfo, OnDungeonLootInfo);
        GameClient.Instance.RegisterHandler(Protocol.DungeonLootChoice, OnDungeonLootChoice);
    }

    private void OnDungeonLootInfo(GamePacket packet)
    {

        var items = packet.DeSerializePayload<List<ItemData>>();
        voteContainer.gameObject.SetActive(true);
        foreach (var item in items)
        {
            var lootChoiceUI = Instantiate(LootChoiceUI, voteContent, false).GetComponent<LootChoiceUI>();
            lootChoices[item.ItemId] = lootChoiceUI;
            lootChoiceUI.Initialize(item);
        }
    }

    private void OnDungeonLootChoice(GamePacket packet)
    {
        var data = packet.DeSerializePayload<ServerDungeonLootChoice>();
        if(!lootChoices.TryGetValue(data.ItemId, out var lootChoice)) return;
        lootChoice.UpdateChoice(data.EntityName, data.LootChoiceType, data.RollValue.ToString());
    }

    private void OnLevelRegionEvent(ServerLevelRegion data)
    {
        UIService.Instance.HidePanel<GameView>();
        SceneService.Instance.LoadThenInvoke($"GameScene_{data.RegionId}", () =>
        {
            Instantiate(ResourceService.Instance.LoadResource<GameObject>("Prefabs/EntityWorld"));
            GameClient.Instance.Send(Protocol.EnterRegion, true);
        });
    }

    private void OnLevelDungeonEvent(ServerLevelDungeon data)
    {
        UIService.Instance.HidePanel<GameView>();
        SceneService.Instance.LoadThenInvoke($"GameScene_{data.RegionId}", () =>
        {
            Instantiate(ResourceService.Instance.LoadResource<GameObject>("Prefabs/EntityWorld"));
            GameClient.Instance.Send(Protocol.EnterRegion, true);
        });
    }

    private void OnPlayerEnterDungeonEvent(ServerPlayerEnterDungeon data)
    {
        UIService.Instance.ShowView<GameView>(onBegin: (view) =>
        {
            Debug.Log(data.LimitTime);
            if (data.LimitTime == 0)
            {
                view.HideLimitTime();
               
            }
            else
            {
                view.ShowLimitTime(data.LimitTime);
            }
        
        });
        EventService.Instance.Publish(this, new PlayerEnterSceneEventArgs{PlayerEntity = data.PlayerEntity});
    }

    public GameObject BuffPrefab;
    public RectTransform BuffContainer;
    
    private Dictionary<int, BuffUI> activeBuffs = new Dictionary<int, BuffUI>();

    public void ApplyBuff(int buffId, float duration)
    {
        var buff = PoolService.Instance.Spawn(BuffPrefab, BuffContainer).GetComponent<BuffUI>();
        activeBuffs[buffId] = buff;
        buff.Initialize(buffId, duration);
    }

    private void OnDestroy()
    {
        controller?.Dispose();
    }

    private void Update()
    {
        controller.Update();

    }
    

    public void ReleaseSkill(int index)
    {
        skillSlots[index].StartCooldown();
    }
    
    
    public void UpdatePlayerHealth(int current, int max)
    {
        hpLiquidMaterial.DOFloat((float)current / max, FillLevel, 0.1f);
    }
    
    public void UpdatePlayerMana(int current, int max)
    {
        mpLiquidMaterial.DOFloat((float)current / max, FillLevel, 0.1f);
    }

    public void ShowSkillInfo(string message)
    {
        SkillInfoRect.gameObject.SetActive(true);
        DescriptionText.text = message;
    }

    public void HideSkillInfo()
    {
        SkillInfoRect.gameObject.SetActive(false);
    }
    
    public void UpdatePlayerExperience(int current, int max)
    {
        if (ExSlider != null)
        {
            ExSlider.DOValue((float)current / max, 0.1f);
        }
    }
    
    // 待完成的功能
    // 1. 进入副本之后会在显示一个框，这个框会展示出所有玩家的蓝量血量，可以选中玩家放技能
    // 2. 当遇到副本boss的时候会显示出dps
    // 3. 左侧显示出正在进行的任务
    
}

