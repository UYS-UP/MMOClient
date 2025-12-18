
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectView : BaseView
{
    private RectTransform characterContainer;
    private GameObject characterCellPrefab;
    
    private Button createNewCharacterButton;
    private Button enterGameButton;
    
    private TMP_Text nicknameText;
    private TMP_Text characterInfoText;
    
    private string currentRoleId;
    
    [SerializeField] private GameObject entityWorldPrefab;

    protected override void Awake()
    {
        base.Awake();
        characterContainer = transform.Find("CharacterContainer").GetComponent<ScrollRect>().content;
        createNewCharacterButton = transform.Find("CreateNewCharacterButton").GetComponent<Button>();
        enterGameButton = transform.Find("EnterGameButton").GetComponent<Button>();
        characterCellPrefab = ResourceService.Instance.LoadResource<GameObject>("Prefabs/UI/RoleButton");
        nicknameText = transform.Find("NicknameText").GetComponent<TMP_Text>();
        characterInfoText = transform.Find("CharacterInfoText").GetComponent<TMP_Text>();
        
        createNewCharacterButton.onClick.AddListener(() =>
        {
            UIService.Instance.HidePanel<CharacterSelectView>();
            ClearRoleList();
            UIService.Instance.ShowView<CharacterCreatView>();
        });
        enterGameButton.onClick.AddListener(OnEnterGameClick);
    }
    
    
    public void AddRole(NetworkCharacter networkCharacter)
    {
        GameObject obj = Instantiate(characterCellPrefab, characterContainer, false);
        TMP_Text text = obj.transform.Find("Info").GetComponent<TMP_Text>();
        text.text = $"角色昵称：{networkCharacter.Name}\n" +
                    $"角色职业: {networkCharacter.Profession}\n" +
                    $"角色等级: {networkCharacter.Level}";
        obj.GetComponent<Button>().onClick.AddListener(() =>
        {
            nicknameText.text = networkCharacter.Name;
            characterInfoText.text = $"角色职业: {networkCharacter.Profession}\n" +
                                $"角色等级: {networkCharacter.Level}\n" +
                                $"角色金币：{networkCharacter.Gold}\n";
            currentRoleId = networkCharacter.CharacterId;
        });
    }
    
    public void ClearRoleList()
    {
        for (int i = 0; i < characterContainer.childCount; i++)
        {
            Destroy(characterContainer.GetChild(i).gameObject);
        }
    }
    
    private void OnEnterGameClick()
    {
        
        //  进入游戏什么都不做，先切场景
        UIService.Instance.HidePanel<CharacterSelectView>();

        SceneService.Instance.LoadThenInvoke("GameScene_001", () =>
        {

            UIService.Instance.ShowView<NotificationView>(layer: UILayer.Toast);
                
            UIService.Instance.ShowView<GameSceneView>(layer: UILayer.Scene);
            UIService.Instance.ShowView<GameView>(layer: UILayer.Normal);
            Instantiate(ResourceService.Instance.LoadResource<GameObject>("Prefabs/EntityWorld"));
            GameClient.Instance.Send(Protocol.EnterGame, currentRoleId);
        });
    }
}
