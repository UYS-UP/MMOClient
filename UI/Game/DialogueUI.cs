// using System;
// using UnityEngine;
// using UnityEngine.UI;
// using System.Linq;
// using TMPro;
//
// public class DialogueUI : MonoBehaviour
// {
//     public TMP_Text speakerText;
//     public TMP_Text dialogueText;
//     public Transform optionsParent;
//     public GameObject optionPrefab;
//
//     private DialogueSystem system;
//
//     public void Init(DialogueSystem system)
//     {
//         this.gameObject.SetActive(true);
//         this.system = system;
//         system.OnNodeChanged += UpdateUI;
//         system.OnDialogueEnd += Hide;
//     }
//
//     private void UpdateUI(DialogueNode nodeModel)
//     {
//         speakerText.text = nodeModel.Speaker;
//         dialogueText.text = nodeModel.Text;
//
//         foreach (Transform child in optionsParent)
//             Destroy(child.gameObject);
//
//         foreach (var option in nodeModel.Options)
//         {
//             var btn = Instantiate(optionPrefab, optionsParent, false);
//             btn.GetComponentInChildren<TMP_Text>().text = option.Text;
//             // 修改监听器，不再传递npcId
//             btn.GetComponent<Button>().onClick.AddListener(() => system.SelectOption(option));
//         }
//     }
//
//     private void Hide() => gameObject.SetActive(false);
// }