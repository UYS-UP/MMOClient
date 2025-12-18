using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FriendItemUI : MonoBehaviour, IPooledObject
{
    public Image avatar;
    public TMP_Text nameText;
    public TMP_Text onlineStateText;
    public HorizontalLayoutGroup  horizontalLayoutGroup;
    public float indentPerLevel = 16f;
    public FriendInfo Friend { get; private set; }

    public void Bind(TreeNode<ITreePayload> node)
    {
        var f = (FriendInfo)node.Data;
        Friend = f;
        if (nameText) nameText.text = f.DisplayName;
        if (onlineStateText) onlineStateText.text = "在线";
        if (avatar){ avatar.enabled = f.Avatar != null; avatar.sprite = f.Avatar; }
        float px = node.Depth * indentPerLevel;
        if (!horizontalLayoutGroup) return;
        var pad = horizontalLayoutGroup.padding;
        pad.left = Mathf.RoundToInt(px);
        horizontalLayoutGroup.padding = pad;
        LayoutRebuilder.MarkLayoutForRebuild(horizontalLayoutGroup.transform as RectTransform);
    }

    public void OnObjectSpawn() {}
    public void OnObjectDespawn() {}
    
}