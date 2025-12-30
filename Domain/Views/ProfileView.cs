
using System;
using UnityEngine;

public class ProfileView : MonoBehaviour
{
    
    private TMPro.TextMeshProUGUI NameText;
    private TMPro.TextMeshProUGUI GuildText;
    private TMPro.TextMeshProUGUI TitleText;
    private UnityEngine.UI.Image AvatarImage;
    private TMPro.TextMeshProUGUI LevelText;
    private TMPro.TextMeshProUGUI ExText;
    private UnityEngine.UI.Image ExImage;

    private void BindComponent()
    {
        var root = this.transform;

        NameText = root.Find("ProfileContainer/InfoContainer/NameText")?.GetComponent<TMPro.TextMeshProUGUI>();

        GuildText = root.Find("ProfileContainer/InfoContainer/GuildText")?.GetComponent<TMPro.TextMeshProUGUI>();

        TitleText = root.Find("ProfileContainer/InfoContainer/TitleText")?.GetComponent<TMPro.TextMeshProUGUI>();

        AvatarImage = root.Find("ProfileContainer/InfoContainer/AvatarFrame/AvatarImage")?.GetComponent<UnityEngine.UI.Image>();

        LevelText = root.Find("ProfileContainer/ExContainer/LevelText")?.GetComponent<TMPro.TextMeshProUGUI>();

        ExText = root.Find("ProfileContainer/ExContainer/ExText")?.GetComponent<TMPro.TextMeshProUGUI>();

        ExImage = root.Find("ProfileContainer/ExContainer/ExBk/ExImage")?.GetComponent<UnityEngine.UI.Image>();

    }

    private NavigationController controller;
    
    private void Awake()
    {
        BindComponent();
    }
    
    public void Initialize(NavigationController controller)
    {
        this.controller = controller;
    }

    public void OpenProfile(string characterName, string characterGuildName, string characterTitle, int currentLevel,
        int maxLevel, float currentEx, float maxEx)
    {
        NameText.text =  $"昵称: {characterName}";
        GuildText.text = $"公会: {characterGuildName}";
        TitleText.text = $"称号: {characterTitle}";
        LevelText.text = $"等级 {currentLevel}/{maxLevel}";
        ExImage.fillAmount = currentEx / maxEx;
        ExText.text = $"{currentEx:N0}/{maxEx:N0}";
    }
}
