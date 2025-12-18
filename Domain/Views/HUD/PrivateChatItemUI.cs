
using System;
using UnityEngine;

public class PrivateChatItemUI : MonoBehaviour, IPooledObject
{
    

    private UnityEngine.UI.Image PicImage;
    private TMPro.TextMeshProUGUI MessageText;
    private UnityEngine.UI.Image MessageAvatarImage;
    private UnityEngine.UI.HorizontalLayoutGroup Horizontal;

    private void BindComponent()
    {
        var root = this.transform;

        {
            var t = root.Find("Bubble/PicImage");
            PicImage = t ? t.GetComponent<UnityEngine.UI.Image>() : null;
        }

        {
            var t = root.Find("Bubble/MessageText");
            MessageText = t ? t.GetComponent<TMPro.TextMeshProUGUI>() : null;
        }

        {
            var t = root.Find("MessageAvatarImage");
            MessageAvatarImage = t ? t.GetComponent<UnityEngine.UI.Image>() : null;
        }

        {
            Horizontal = root.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        }

    }


    private void Awake()
    {
        BindComponent();
        
    }

    public void Init(string content, bool isMe)
    {
        PicImage.gameObject.SetActive(false);
        Horizontal.childAlignment = isMe ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
        if (isMe)
        {
            MessageAvatarImage.transform.SetAsFirstSibling();
        }
        else
        {
            MessageAvatarImage.transform.SetAsLastSibling();
        }
        
        MessageText.text = content;
    }

    public void OnObjectSpawn()
    {
        
    }

    public void OnObjectDespawn()
    {
        // 清理内容
        PicImage.sprite = null;
        MessageAvatarImage.sprite = null;
        MessageText.text = "";
    }
}
