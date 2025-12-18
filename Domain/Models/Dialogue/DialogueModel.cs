using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 对话系统（NPC → 任务列表 → 多阶段对话）
/// </summary>
public class DialogueModel : IDisposable
{
    private QuestModel questModel;
    private NpcDialogueData currentNpcData;
    private QuestDialogueGroup currentQuestGroup;
    private StageDialogue currentStage;
    public DialogueNode CurrentNode { get; set; }
    public string CurrentNpcId { get; set; }

    // NPC 对话缓存：npcId → dialogueData
    private readonly Dictionary<string, NpcDialogueData> npcDialogueCache = new();

    public event Action<bool> OnDialogueTip;
    public event Action<DialogueNode> OnNodeChanged;
    public event Action OnDialogueEnd;
    

    public DialogueModel()
    {
        EventService.Instance.Subscribe<TriggerEnterNpcEventArgs>(this, OnEnterNpc);
        EventService.Instance.Subscribe<TriggerExitNpcEventArgs>(this, OnExitNpc);
        questModel = GameContext.Instance.Get<QuestModel>();
    }

    private void OnEnterNpc(object sender, TriggerEnterNpcEventArgs e)
    {
        CurrentNpcId = e.NpcId;
        OnDialogueTip?.Invoke(true);
    }

    private void OnExitNpc(object sender, TriggerExitNpcEventArgs e)
    {
        if (CurrentNpcId == e.NpcId)
        {
            CurrentNpcId = null;
            OnDialogueTip?.Invoke(false);
        }
    }

    /// <summary>
    /// 开始与 NPC 对话
    /// 自动从 NPC 文件中选择最合适的任务对话 / 默认对话
    /// </summary>
    public void StartDialogue()
    {
        if (string.IsNullOrEmpty(CurrentNpcId)) return; 

        var npcData = LoadNpcDialogue(CurrentNpcId);
        if (npcData == null)
        {
            Debug.LogWarning($"[Dialogue] NPC {CurrentNpcId} 无对话文件");
            EndDialogue();
            return;
        }

        currentNpcData = npcData;

        // 先查任务相关对话
        var result = SelectBestDialogueStage(npcData, CurrentNpcId);

        if (result.group != null && result.stage != null)
        {
            currentQuestGroup = result.group;
            currentStage = result.stage;
            CurrentNode = LoadStartNode(currentStage);

            OnNodeChanged?.Invoke(CurrentNode);
        }

        // 没任务 → 默认（none）
        if (npcData.QuestDialogues.TryGetValue("none", out var defaultGroup))
        {
            currentQuestGroup = defaultGroup;
            currentStage = defaultGroup.Stages.FirstOrDefault(s => s.TriggerCondition == "Default")
                           ?? defaultGroup.Stages.First();

            CurrentNode = LoadStartNode(currentStage);
            OnNodeChanged?.Invoke(CurrentNode);
        }

        EndDialogue();
    }

    /// <summary>
    /// 用户选择对话选项
    /// </summary>
    public DialogueNode SelectOption(DialogueOption option)
    {
        // 推进任务
        if (!string.IsNullOrEmpty(option.AdvanceToQuestNode))
        {
            questModel.RequestAdvanceQuestNode(CurrentNpcId, option.AdvanceToQuestNode);
        }

        // 对话跳转
        if (!string.IsNullOrEmpty(option.NextDialogueNode))
        {
            var next = currentStage.Nodes.FirstOrDefault(n => n.NodeId == option.NextDialogueNode);
            if (next != null)
            {
                CurrentNode = next;
                OnNodeChanged?.Invoke(CurrentNode);

                if (!string.IsNullOrEmpty(next.AdvanceToQuestNode))
                {
                    questModel.RequestAdvanceQuestNode(CurrentNpcId, next.AdvanceToQuestNode);
                }
                return CurrentNode;
            }
        }

        EndDialogue();
        return null;
    }

    public void EndDialogue()
    {
        currentNpcData = null;
        currentQuestGroup = null;
        currentStage = null;
        CurrentNode = null;
        // 不清理 currentNpcId，这样退出范围时依旧能关闭提示
        OnDialogueEnd?.Invoke();
    }

    // =======================================================================
    // 核心：根据 NPC + 玩家任务状态，选择最合适的对话
    // =======================================================================

    private (QuestDialogueGroup group, StageDialogue stage)
        SelectBestDialogueStage(NpcDialogueData npcData, string npcId)
    {
        foreach (var quest in questModel.GetAllQuests())
        {
            // 任务 NodeId 是否在 NPC 对话里
            if (!npcData.QuestDialogues.TryGetValue(quest.NodeId, out var group))
                continue;

            var stage = FindMatchingStage(group.Stages, quest, npcId);
            if (stage != null)
                return (group, stage);
        }

        return (null, null);
    }

    private StageDialogue FindMatchingStage(List<StageDialogue> stages, QuestNode quest, string npcId)
    {
        StageDialogue Try(string cond) =>
            stages.FirstOrDefault(s => 
                string.Equals(s.TriggerCondition, cond, StringComparison.OrdinalIgnoreCase));

        // 1. 可提交
        if (quest.Objectives.Any(o =>
            o.Type == ObjectiveType.SubmitToNpc &&
            o.TargetId == npcId &&
            o.IsCompleted))
            return Try("Submittable");

        // 2. 可接（初始状态 + 存在 TalkToNpc）
        if (quest.Objectives.All(o => o.CurrentCount == 0) &&
            quest.Objectives.Any(o => o.Type == ObjectiveType.TalkToNpc && o.TargetId == npcId))
            return Try("Accept");

        // 3. 进行中
        if (!quest.IsCompleted() &&
            quest.Objectives.Any(o =>
                (o.Type == ObjectiveType.TalkToNpc || o.Type == ObjectiveType.SubmitToNpc)
                && o.TargetId == npcId))
            return Try("InProgress");

        // 4. 已完成
        if (quest.IsCompleted())
            return Try("Completed");

        return null;
    }

    private DialogueNode LoadStartNode(StageDialogue stage)
    {
        return stage.Nodes.FirstOrDefault(n => n.NodeId == stage.StartNodeId)
               ?? stage.Nodes.First();
    }

    // =======================================================================
    // 加载 NPC 对话文件
    // =======================================================================

    private NpcDialogueData LoadNpcDialogue(string npcId)
    {
        if (npcDialogueCache.TryGetValue(npcId, out var cached))
            return cached;

        var asset = Resources.Load<TextAsset>($"Dialogues/NPC/{npcId}");
        if (asset == null)
            return null;

        var data = JsonConvert.DeserializeObject<NpcDialogueData>(asset.text);
        npcDialogueCache[npcId] = data;
        return data;
    }

    public void Dispose()
    {
        EventService.Instance.Unsubscribe(this);
    }
}
