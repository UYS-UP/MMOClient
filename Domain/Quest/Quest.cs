using System;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;

[MessagePackObject]
public class QuestNode
{
    [Key(0)] public string NodeId { get; set; }                 // 唯一ID，如 "main_001_01"
    [Key(1)] public string QuestName { get; set; }
    [Key(2)] public string Description { get; set; }
    [Key(3)] public List<QuestObjective> Objectives { get; set; } = new(); // 当前节点目标
    [Key(4)] public List<string> NextNodeIds { get; set; } = new();        // 成功后可进入的节点
    [Key(5)] public List<string> FailNodeIds { get; set; } = new();         // 失败跳转（可选）
    [Key(6)] public bool IsBranchStart { get; set; } = false;              // 是否是分支起点
    [Key(7)] public bool AutoAccept { get; set; } = true;
    [Key(8)] public bool AutoSubmit { get; set; } = true;

    public bool IsCompleted()
    {
        bool isCompleted = true;
        foreach (var objective in Objectives)
        {
            if(objective.IsCompleted) continue;
            isCompleted = false;
        }
        return isCompleted;
    }
}

[MessagePackObject]
public class QuestObjective
{
    [Key(0)] public ObjectiveType Type { get; set; }
    [Key(1)] public string TargetId { get; set; }      // 怪物ID、物品ID、NPC ID、区域ID
    [Key(2)] public int RequireCount { get; set; } = 1;
    [Key(3)] public int CurrentCount { get; set; } = 0;

    [IgnoreMember] public bool IsCompleted => CurrentCount >= RequireCount;
}

public enum ObjectiveType
{
    KillMonster,
    CollectItem,
    TalkToNpc,
    EnterRegion,
    UseSkill,
    ReachLevel,
    SubmitToNpc,
    CustomEvent
}