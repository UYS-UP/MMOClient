using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;


[Serializable]
public class NpcDialogueData
{
    public string NpcId;

    // key: questNodeId 或 "none"
    public Dictionary<string, QuestDialogueGroup> QuestDialogues;
}

[Serializable]
public class QuestDialogueGroup
{
    public List<StageDialogue> Stages;
}

[Serializable]
public class StageDialogue
{
    public string TriggerCondition; // Accept / InProgress / Submittable / Completed / Default
    public string StartNodeId = "start";
    public List<DialogueNode> Nodes;
}