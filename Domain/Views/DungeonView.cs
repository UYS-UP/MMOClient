using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DungeonView : BaseView
{
    private RectTransform selectDungeonRect;
    private RectTransform dungeonTeamRect;

    private RectTransform dungeonListRect;
    private GameObject togglePrefab;
    private GameObject teamMemberPrefab;
    
    private TMP_Text dungeonNameText;
    private TMP_Text dungeonLimitText;
    private Image dungeonImage;
    private Button createButton;
    private List<Toggle> dungeonToggles;
    
    private TMP_Text teamDungeonNameText;
    private TMP_Text teamDungeonLimitText;
    private Button inviteRegionButton;
    private Button enterDungeonButton;
    private RectTransform teamMemberRect;
    private List<TeamMemberUI> teamMembers;
    
    private string currentDungeon;

    private DungeonController controller;

    protected override void Awake()
    {
        base.Awake();
        controller = new DungeonController(this);
        selectDungeonRect = transform.Find("SelectDungeon").GetComponent<RectTransform>();
        dungeonTeamRect = transform.Find("DungeonTeam").GetComponent<RectTransform>();
        dungeonListRect = selectDungeonRect.Find("DungeonList").GetComponent<RectTransform>();
        togglePrefab = ResourceService.Instance.LoadResource<GameObject>("Prefabs/UI/DungeonToggle");
        
        dungeonNameText = selectDungeonRect.Find("DungeonName").GetComponent<TextMeshProUGUI>();
        dungeonLimitText = selectDungeonRect.Find("DungeonLimit").GetComponent<TextMeshProUGUI>();
        dungeonImage = selectDungeonRect.Find("DungeonImage").GetComponent<Image>();
        createButton = selectDungeonRect.Find("CreateButton").GetComponent<Button>();
        dungeonImage = selectDungeonRect.Find("DungeonImage").GetComponent<Image>();
        dungeonToggles = new List<Toggle>();
        
        teamMemberRect = dungeonTeamRect.Find("TeamMembers").GetComponent<RectTransform>();
        inviteRegionButton = dungeonTeamRect.Find("InviteRegionBtn").GetComponent<Button>();
        enterDungeonButton = dungeonTeamRect.Find("EnterDungeonBtn").GetComponent<Button>();
        teamMemberPrefab = ResourceService.Instance.LoadResource<GameObject>("Prefabs/UI/TeamMemberUI");
        
        Initialize();
    }

    private void Start()
    {
        ProtocolRegister.Instance.OnLoadDungeonEvent += OnLoadDungeonEvent;
        
    }

    private void OnDestroy()
    {
        ProtocolRegister.Instance.OnLoadDungeonEvent -= OnLoadDungeonEvent; 
        
    }

    private void OnLoadDungeonEvent(ServerLoadDungeon data)
    {
        UIService.Instance.HidePanel<GameView>();
        UIService.Instance.HidePanel<DungeonView>(onComplete: (view) => InputBindService.Instance.UIIsOpen = false);
        SceneService.Instance.LoadThenInvoke($"GameScene_{data.TemplateId}", () =>
        {
            Instantiate(ResourceService.Instance.LoadResource<GameObject>("Prefabs/EntityWorld"));
            GameClient.Instance.Send(Protocol.EnterDungeon, false);
        });
    } 

    private void Initialize()
    {
        teamMembers = new List<TeamMemberUI>();
        dungeonTeamRect.gameObject.SetActive(false);
        var templates = DungeonTemplateConfig.GetTemplates(0);
        foreach (var dungeonTemplate in templates)
        {
            var obj = Instantiate(togglePrefab, dungeonListRect, false);
            var toggle = obj.GetComponent<Toggle>();
            var text = toggle.GetComponentInChildren<TMP_Text>();
            text.text = dungeonTemplate.Name;
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn) return;
                dungeonNameText.text = dungeonTemplate.Name;
                var level = dungeonTemplate.MinLevel == 0 ? "无限制" : dungeonTemplate.MinLevel.ToString();
                dungeonLimitText.text = $"人数: {dungeonTemplate.MinLevel}至{dungeonTemplate.MaxPlayers}人\n等级: {level}级";
                currentDungeon = dungeonTemplate.Id;
            });
            dungeonToggles.Add(toggle);
        }

        if (dungeonToggles.Count > 0)
        {
            dungeonToggles[0].isOn = false;
            dungeonToggles[0].isOn = true;
        }
        createButton.onClick.AddListener(() =>
        {
            GameClient.Instance.Send(Protocol.CreateDungeonTeam, new ClientCreateDungeonTeam
            {
                TeamName = "副本大王",
                TemplateId = currentDungeon
            });
        });
        enterDungeonButton.onClick.AddListener(() =>
        {
            Debug.Log("Enter Dungeon");
            controller.StartDungeon();
        });
        inviteRegionButton.onClick.AddListener(() => controller.InviteRegion());
    }
    

    public void CreateTeam(List<TeamMember> members)
    {
        if (!DungeonTemplateConfig.TryGetTemplateById(currentDungeon, out var template)) return;
        EnsureTeamMemberSlots(template.MaxPlayers);
        PopulateTeamMembers(members);
        ShowTeamPanel();
    }
    

    public void UpdateTeam(int maxPlayers, List<TeamMember> members)
    {
        EnsureTeamMemberSlots(maxPlayers);   
        PopulateTeamMembers(members);
    }

    public void TeamJoined(int maxPlayers, List<TeamMember> members)
    {
        enterDungeonButton.gameObject.SetActive(false);
        EnsureTeamMemberSlots(maxPlayers);
        PopulateTeamMembers(members);
        ShowTeamPanel();
    }
    
    private void EnsureTeamMemberSlots(int maxPlayers)
    {
        // 只补足，不重复造
        while (teamMembers.Count < maxPlayers)
        {
            var obj = Instantiate(teamMemberPrefab, teamMemberRect, false);
            teamMembers.Add(obj.GetComponent<TeamMemberUI>());
        }
        // 多余的隐藏即可（避免销毁/重建带来的 GC）
        for (int i = 0; i < teamMembers.Count; i++)
        {
            teamMembers[i].ActiveInvite();
        }
    }

    private void PopulateTeamMembers(IList<TeamMember> members)
    {
        // 把有人的位置激活并写入名字，其余位置隐藏避免显示旧数据
        for (int i = 0; i < teamMembers.Count; i++)
        {
            if (i < members.Count)
            {
                teamMembers[i].gameObject.SetActive(true);
                teamMembers[i].ActiveInfo(members[i].Name);
            }
            else
            {
                teamMembers[i].ActiveInvite();
            }
        }
    }

    private void ShowTeamPanel()
    {
        selectDungeonRect.gameObject.SetActive(false);
        dungeonTeamRect.gameObject.SetActive(true);
    }
}
