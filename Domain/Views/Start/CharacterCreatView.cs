
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class CharacterCreatView : BaseView
{
    private Dictionary<Toggle, ProfessionType> roleTypeToggles;
    
    
    private UnityEngine.UI.Button CreateCharacterButton;
    private TMPro.TMP_InputField CharacterNameInput;
    private TMPro.TextMeshProUGUI TipText;

    private void BindComponent()
    {
        var root = this.transform;

        {
            var t = root.Find("CreateCharacterButton");
            CreateCharacterButton = t ? t.GetComponent<UnityEngine.UI.Button>() : null;
        }

        {
            var t = root.Find("CharacterNameInput");
            CharacterNameInput = t ? t.GetComponent<TMPro.TMP_InputField>() : null;
        }

        {
            var t = root.Find("TipText");
            TipText = t ? t.GetComponent<TMPro.TextMeshProUGUI>() : null;
        }

    }


    protected override void Awake()
    {
        base.Awake();
        BindComponent();
        
        CreateCharacterButton.onClick.AddListener(OnCreateCharacterClick);
        ProtocolRegister.Instance.OnCreateCharacterResponseEvent += OnCreateCharacterResponseEvent;
    }

    private void OnCreateCharacterClick()
    {
        var payload = new ClientCreateCharacter
        {
            CharacterName = CharacterNameInput.text,
            Profession = ProfessionType.Mage
        };
        GameClient.Instance.Send(Protocol.CreateCharacter, payload);
    }

    private void OnCreateCharacterResponseEvent(ResponseMessage<List<NetworkCharacter>> data)
    {
        if (data.Code == StateCode.Success)
        {
            UIService.Instance.HidePanel<CharacterCreatView>();
            UIService.Instance.ShowView<CharacterSelectView>((panel) =>
            {
                foreach (var role in data.Data)
                {
                    panel.AddRole(role);
                }
            });
        }
        else
        {
            TipText.text = data.Message;
        }
    }
    
}
