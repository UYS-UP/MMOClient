using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueNode
{
    public string NodeId { get; set; }
    public string Speaker { get; set; }
    public string Text { get; set; }
    public List<DialogueOption> Options { get; set; } = new();
    public string AdvanceToQuestNode { get; set; }
}