using System;
using UnityEngine;

public class FriendRequestItemUI : MonoBehaviour
{
    
    private UnityEngine.UI.Button AcceptButton;
    private UnityEngine.UI.Button RefuseButton;
    private TMPro.TextMeshProUGUI MessageText;
    private TMPro.TextMeshProUGUI RequestText;
    
    private string requestId;
    private void BindComponent()
    {
        var root = this.transform;

        {
            var t = root.Find("AcceptButton");
            AcceptButton = t ? t.GetComponent<UnityEngine.UI.Button>() : null;
        }

        {
            var t = root.Find("RefuseButton");
            RefuseButton = t ? t.GetComponent<UnityEngine.UI.Button>() : null;
        }

        {
            var t = root.Find("MessageText");
            MessageText = t ? t.GetComponent<TMPro.TextMeshProUGUI>() : null;
        }

        {
            var t = root.Find("RequestText");
            RequestText = t ? t.GetComponent<TMPro.TextMeshProUGUI>() : null;
        }

    }

    private void Awake()
    {
        BindComponent();
        AcceptButton.onClick.AddListener(OnAcceptButtonClick);
        RefuseButton.onClick.AddListener(OnRefuseButtonClick);
    }
    
    public void Init(string requestId, string senderName, string remark)
    {
        this.requestId = requestId;
        RequestText.text = $"{senderName} 请求添加你为好友";
        MessageText.text = remark;
    }

    private void OnRefuseButtonClick()
    {
        FriendModel.Instance.HandleAddFriendRequest(requestId, false);
        Destroy(gameObject);
    }

    private void OnAcceptButtonClick()
    {
        FriendModel.Instance.HandleAddFriendRequest(requestId, true);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        AcceptButton.onClick.RemoveListener(OnAcceptButtonClick);
        RefuseButton.onClick.RemoveListener(OnRefuseButtonClick);
    }
}
