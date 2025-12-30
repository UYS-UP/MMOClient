
using System;
using DG.Tweening;
using UnityEngine;

public class ItemAcquiredNotifiycation : MonoBehaviour, IPooledObject
{

    public RectTransform Rect;
    public UnityEngine.CanvasGroup CanvasGroup;
    
    private TMPro.TextMeshProUGUI Name;
    private UnityEngine.UI.Image Icon;
    
    
    public void UpdateNotification(string name, Sprite icon)
    {
        Name.text = name;
        Icon.sprite = icon;
    }

    private void Awake()
    {
        BindComponent();
    }
    
    private void BindComponent()
    {
        var root = this.transform;

        {
            var t = root.Find("");
            Rect = t ? t.GetComponent<UnityEngine.RectTransform>() : null;
        }

        {
            var t = root.Find("Name");
            Name = t ? t.GetComponent<TMPro.TextMeshProUGUI>() : null;
        }

        {
            var t = root.Find("Icon");
            Icon = t ? t.GetComponent<UnityEngine.UI.Image>() : null;
        }

        {
            CanvasGroup = transform.GetComponent<UnityEngine.CanvasGroup>();
        }

    }


    
    public void OnObjectSpawn()
    {

    }

    public void OnObjectDespawn()
    {
        
    }
}
