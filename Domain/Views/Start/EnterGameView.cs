
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnterGameView : BaseView
{


    private UnityEngine.RectTransform SelectContainerRect;
    private UnityEngine.RectTransform CreateContainerRect;
    private UnityEngine.UI.Button EnterGameButton;
    private UnityEngine.RectTransform ServerContainerRect;
    private UnityEngine.UI.Button CreateCharacterButton;
    private TMPro.TMP_InputField CharacterNameInput;
    private TMPro.TextMeshProUGUI TipText;

    private void BindComponent()
    {
        var root = this.transform;

        SelectContainerRect = root.Find("SelectContainer") as RectTransform;

        CreateContainerRect = root.Find("CreateContainer") as RectTransform;

        EnterGameButton = root.Find("SelectContainer/EnterGameButton")?.GetComponent<UnityEngine.UI.Button>();

        ServerContainerRect = root.Find("SelectContainer/ServerContainer") as RectTransform;

        CreateCharacterButton = root.Find("CreateContainer/CreateCharacterButton")?.GetComponent<UnityEngine.UI.Button>();

        CharacterNameInput = root.Find("CreateContainer/CharacterNameInput")?.GetComponent<TMPro.TMP_InputField>();

        TipText = root.Find("TipText")?.GetComponent<TMPro.TextMeshProUGUI>();

    }


    private ServerItem currentSelectServer;
    private GameObject ServerItemPrefab;
    private readonly Dictionary<int, ServerItem> ServerItems = new Dictionary<int, ServerItem>();

    private EnterGameController controller;
    protected override void Awake()
    {
        base.Awake();
        BindComponent();
        EnterGameButton.onClick.AddListener(OnEnterGameClick);
        CreateCharacterButton.onClick.AddListener(OnCreateCharacterClick);
        TipText.text = string.Empty;
        ServerItemPrefab =
            ResourceService.Instance.LoadResource<GameObject>("Prefabs/UI/Modules/Server/ServerItem");
        CreateContainerRect.gameObject.SetActive(false);
        for (int i = 0; i < 1; i++)
        {
            var serverItem = Instantiate(ServerItemPrefab, ServerContainerRect).GetComponent<ServerItem>();
            serverItem.InitializeServer(i, "嚎哭深渊");
            serverItem.OnSelectToggleChanged += OnSelectToggleChanged;
            ServerItems[i] = serverItem;
        }

        controller = new EnterGameController(this);

    }

    private void OnSelectToggleChanged(bool isSelected, ServerItem serverItem)
    {
        if (isSelected)
        {
            currentSelectServer = serverItem;
            foreach (var item in ServerItems)
            {
                if(item.Key == serverItem.ServerId) continue;
                item.Value.gameObject.SetActive(false);
            }
        }
        else
        {
            var scale = ServerContainerRect.localScale;
            scale.x = 0;
            ServerContainerRect.localScale = scale;
            ServerContainerRect.DOScaleX(1, 1f).SetEase(Ease.OutBack);
        }
    }
    
    
    public void AddCharacter(List<NetworkCharacterPreview> characters)
    {
        foreach (var character in characters)
        {
            if (ServerItems.TryGetValue(character.ServerId, out var item))
            {
                item.InitializeCharacter(character.CharacterId, character.CharacterName, character.Level);
            }
        }
    }

    public void ShowTip(string tip)
    {
        TipText.text = tip;
    }
    
    private void OnEnterGameClick()
    {
        if (currentSelectServer == null)
        {
            TipText.text = "请先选择一个服务器";
            return;
        }
        if (string.IsNullOrEmpty(currentSelectServer.CharacterId))
        {
            SelectContainerRect.gameObject.SetActive(false);
            CreateContainerRect.gameObject.SetActive(true);
            return;
        }
        UIService.Instance.HidePanel<EnterGameView>();
        UIService.Instance.ShowView<NotificationView>(layer: UILayer.Toast);
        UIService.Instance.ShowView<GameSceneView>(layer: UILayer.Scene);
        UIService.Instance.ShowView<GameView>(layer: UILayer.Normal);
        UIService.Instance.ShowView<GMView>(layer: UILayer.System);
        GameClient.Instance.Send(Protocol.CS_EnterGame, new ClientEnterGame {CharacterId = currentSelectServer.CharacterId});
    }

    private void OnCreateCharacterClick()
    {

        var payload = new ClientCreateCharacter
        {
            CharacterName = CharacterNameInput.text,
            ServerId = currentSelectServer.ServerId
        };
        
        GameClient.Instance.Send(Protocol.CS_CreateCharacter, payload);
    }
    
}
