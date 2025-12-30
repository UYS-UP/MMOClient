
using System;
using UnityEngine;

public class ServerItem : MonoBehaviour
{
    private TMPro.TextMeshProUGUI LevelText;
    private TMPro.TextMeshProUGUI NicknameText;
    private UnityEngine.UI.Toggle ServerToggle;
    private TMPro.TextMeshProUGUI ServerNameText;

    private void BindComponent()
    {
        var root = this.transform;

        LevelText = root.Find("LevelText")?.GetComponent<TMPro.TextMeshProUGUI>();

        NicknameText = root.Find("NicknameText")?.GetComponent<TMPro.TextMeshProUGUI>();

        ServerToggle = root.Find("ServerToggle")?.GetComponent<UnityEngine.UI.Toggle>();

        ServerNameText = root.Find("ServerToggle/ServerNameText")?.GetComponent<TMPro.TextMeshProUGUI>();

    }

    
    public int ServerId;
    public string CharacterId;
    public event Action<bool, ServerItem> OnSelectToggleChanged;

    private void Awake()
    {
        BindComponent();
        ServerToggle.onValueChanged.AddListener(OnServerButtonClick);
    }

    public void InitializeServer(int serverId, string serverName)
    {
        ServerId = serverId;
        ServerNameText.text = serverName;
    }

    public void InitializeCharacter(string characterId, string characterName, int characterLevel)
    {
        CharacterId = characterId;
        NicknameText.text = characterName;
        LevelText.text = $"LEVEL {characterLevel}";
    }

    private void OnServerButtonClick(bool isSelected)
    {
        OnSelectToggleChanged?.Invoke(isSelected, this);
    }
}
