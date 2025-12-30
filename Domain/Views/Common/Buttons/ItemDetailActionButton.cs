
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailActionButton : MonoBehaviour, IPooledObject
{
    private Button button;
    private Image bg;
    private TextMeshProUGUI text;
    
    
    private void Awake()
    {
        button = GetComponent<Button>();
        bg = GetComponent<Image>();
        text = button.GetComponentInChildren<TextMeshProUGUI>();
     
    }

    public void Initialize(string btnText, Action onClick, Color bgColor)
    {
        button.onClick.RemoveAllListeners();   
        button.onClick.AddListener(() => onClick?.Invoke());
        text.text = btnText;
        bg.color = bgColor;
    }

    public void OnObjectSpawn()
    {
        
    }

    public void OnObjectDespawn()
    {
        button.onClick.RemoveAllListeners();   
    }
}
