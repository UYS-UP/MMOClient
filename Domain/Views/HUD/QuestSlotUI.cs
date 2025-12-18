
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class QuestSlotUI : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    private readonly List<QuestObjectiveUI>  questObjectives = new List<QuestObjectiveUI>();
    public GameObject ObjectiveUI;
    public CanvasGroup canvasGroup;
    public RectTransform objectiveContainer;
    
    public void UpdateQuest(QuestNode node)
    {
        titleText.text = $"[{node.QuestName}]{node.Description}: ";
        foreach(var obj in questObjectives)
            Destroy(obj.gameObject);
        foreach (var objective in node.Objectives)
        {
            var questObjective = Instantiate(ObjectiveUI, objectiveContainer, false).GetComponent<QuestObjectiveUI>();
            questObjectives.Add(questObjective);
            string description;
            if (objective.Type == ObjectiveType.SubmitToNpc || objective.Type == ObjectiveType.TalkToNpc)
            {
                description = $"{objective.Type}: <link='{objective.Type}'><color=#0000FF><u>{objective.TargetId}</u></color></link>";
            }
            else
            {
                description = $"{objective.Type}: {objective.CurrentCount}/{objective.RequireCount}";
            }
            questObjective.UpdateQuestObjective(objective.IsCompleted, description);
        }
    }


    public void UpdateQuest(int index, QuestObjective questObjective)
    {
        if(questObjectives.Count <= index) return;
        string description;
        if (questObjective.Type == ObjectiveType.SubmitToNpc || questObjective.Type == ObjectiveType.TalkToNpc)
        {
            description = $"{questObjective.Type}: <link='{questObjective.Type}'><color=#0000FF><u>{questObjective.TargetId}</u></color></link>";
        }
        else
        {
            description = $"{questObjective.Type}: {questObjective.CurrentCount}/{questObjective.RequireCount}";
        }
        questObjectives[index].UpdateQuestObjective(questObjective.IsCompleted, description);
    }
}
