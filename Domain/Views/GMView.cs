
using UnityEngine;

public class GMView : BaseView
{
    
    
    private UnityEngine.UI.Toggle GMToggle;
    private UnityEngine.RectTransform GMContainerRect;
    private UnityEngine.UI.Button AddItemButton;
    private TMPro.TMP_InputField ItemIdInput;

    private void BindComponent()
    {
        var root = this.transform;

        GMToggle = root.Find("GMToggle")?.GetComponent<UnityEngine.UI.Toggle>();

        GMContainerRect = root.Find("GMContainer") as RectTransform;

        AddItemButton = root.Find("GMContainer/GMAddItem/AddItemButton")?.GetComponent<UnityEngine.UI.Button>();

        ItemIdInput = root.Find("GMContainer/GMAddItem/ItemIdInput")?.GetComponent<TMPro.TMP_InputField>();

    }

    
    private GMController controller;
    
    protected override void Awake()
    {
        base.Awake();
        BindComponent();
        controller = new GMController();  
        AddItemButton.onClick.AddListener(OnClickAddItem);
        GMContainerRect.gameObject.SetActive(false);
        GMToggle.onValueChanged.AddListener((isSelected) =>
        {
            GMContainerRect.gameObject.SetActive(isSelected);
        });
    }

    private void OnClickAddItem()
    {
        if(string.IsNullOrEmpty(ItemIdInput.text)) return;
        controller.GMAddItem(ItemIdInput.text);
    }
}
