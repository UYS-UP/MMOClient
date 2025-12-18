
using System;
using UnityEngine;

public class QuestObjectiveUI : MonoBehaviour
{
    
    private UnityEngine.UI.Image CompletedImage;
    private TMPro.TextMeshProUGUI ObjectiveText;
    
    public Sprite CompletedSprite;
    public Sprite FailedSprite;

    private void BindComponent()
    {
        var root = this.transform;

        CompletedImage = root.Find("CompletedImage")?.GetComponent<UnityEngine.UI.Image>();

        ObjectiveText = root.Find("ObjectiveText")?.GetComponent<TMPro.TextMeshProUGUI>();

    }

    private void Awake()
    {
        BindComponent();
    }

    public void UpdateQuestObjective(bool isCompleted, string objective)
    {
        CompletedImage.sprite = isCompleted ? CompletedSprite : FailedSprite;
        Debug.Log(objective);
        ObjectiveText.text = objective;
    }
}
