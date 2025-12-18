using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GroupItemUI : MonoBehaviour, IPooledObject
{
    public TMP_Text nameText;
    public Image arrow;
    public Button clickArea;
    public float indentPerLevel = 16f;
    public HorizontalLayoutGroup  horizontalLayoutGroup;
    public GroupInfo Group {get; private set;}
    void Awake(){ if(!clickArea) clickArea = GetComponent<Button>(); }

    public void Bind(TreeNode<ITreePayload> node, System.Action onToggle)
    {
        var g = (GroupInfo)node.Data;
        Group = g;
        if (nameText) nameText.text = g.Name;
        if (arrow){ arrow.gameObject.SetActive(true); arrow.rectTransform.localRotation = node.IsExpanded? Quaternion.Euler(0,0,-90f): Quaternion.identity; }
        float px = node.Depth * indentPerLevel;
        if (clickArea){
            clickArea.gameObject.SetActive(node.HasChildren);
            clickArea.onClick.RemoveAllListeners();
            clickArea.onClick.AddListener(() =>
            {
                onToggle?.Invoke(); 
                if (arrow) 
                    arrow.rectTransform.localRotation = node.IsExpanded? Quaternion.Euler(0,0,-90f): Quaternion.identity;
            });
        }
        
        var pad = horizontalLayoutGroup.padding;
        pad.left = Mathf.RoundToInt(px);
        horizontalLayoutGroup.padding = pad;
        LayoutRebuilder.MarkLayoutForRebuild(horizontalLayoutGroup.transform as RectTransform);
    }
    public void OnObjectSpawn() {}
    public void OnObjectDespawn(){ if (clickArea) clickArea.onClick.RemoveAllListeners(); }
}