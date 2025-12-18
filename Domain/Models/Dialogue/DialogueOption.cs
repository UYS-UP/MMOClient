[System.Serializable]
public class DialogueOption
{
    public string Text { get; set; }
    public string NextDialogueNode { get; set; }        // 传统跳转
    public string AdvanceToQuestNode { get; set; }      // 选了这个选项后，推进任务到指定节点
}