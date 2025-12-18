
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum LootChoiceType
{
    Pending = 0,  // 还没选择
    Pass = 1,     // 放弃
    Rolled = 2,   // 已经 ROLL 点
}

public class LootChoiceUI : MonoBehaviour
{
    public Image ItemImage;
    public Button RollButton;
    public Button PassButton;
    public RectTransform ChoiceRect;
    public GameObject choicePrefab;
    private string itemId;
    
    public void Initialize(ItemData item)
    {
        itemId = item.ItemId;
        RollButton.onClick.AddListener(() =>
        {
            GameClient.Instance.Send(Protocol.DungeonLootChoice, new ClientDungeonLootChoice
            {
                IsRoll = true,
                ItemId = itemId,
            });
            RollButton.interactable = false;
            PassButton.interactable = false;
        });
        
        PassButton.onClick.AddListener(() =>
        {
            GameClient.Instance.Send(Protocol.DungeonLootChoice, new ClientDungeonLootChoice
            {
                IsRoll = false,
                ItemId = itemId,
            });
            RollButton.interactable = false;
            PassButton.interactable = false;
        });
    }

    public void UpdateChoice(string entityName, LootChoiceType choiceType, string roll = "")
    {
        if (choiceType == LootChoiceType.Pass)
        {
            Instantiate(choicePrefab, this.transform, false).GetComponentInChildren<TextMeshProUGUI>().text = $"{entityName}: 弃权";
        }else if (choiceType == LootChoiceType.Rolled)
        {
            Instantiate(choicePrefab, this.transform, false).GetComponentInChildren<TextMeshProUGUI>().text = $"{entityName}: {roll}点";
        }
    }
    
}
