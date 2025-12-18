
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamMemberUI : MonoBehaviour
{
    [SerializeField] private Button inviteButton;
    [SerializeField] private RectTransform infoRect;
    [SerializeField] private TMP_Text nameText;

    private void Awake()
    {
        if (inviteButton == null || infoRect == null || nameText == null)
        {
            inviteButton = transform.Find("InviteButton").GetComponent<Button>(); 
            infoRect = transform.Find("Info").GetComponent<RectTransform>();
            nameText = infoRect.Find("Name").GetComponent<TMP_Text>();
        }
    }

    public void ActiveInfo(string name)
    {
        if (inviteButton == null || infoRect == null || nameText == null)
        {
            inviteButton = transform.Find("InviteButton").GetComponent<Button>(); 
            infoRect = transform.Find("Info").GetComponent<RectTransform>();
            nameText = infoRect.Find("Name").GetComponent<TMP_Text>();
        }
        inviteButton.gameObject.SetActive(false);
        nameText.text = name;
        infoRect.gameObject.SetActive(true);
        
    }

    public void ActiveInvite()
    {
        if (inviteButton == null || infoRect == null || nameText == null)
        {
            inviteButton = transform.Find("InviteButton").GetComponent<Button>(); 
            infoRect = transform.Find("Info").GetComponent<RectTransform>();
            nameText = infoRect.Find("Name").GetComponent<TMP_Text>();
        }
        infoRect.gameObject.SetActive(false);
        inviteButton.gameObject.SetActive(true);
    }
}
