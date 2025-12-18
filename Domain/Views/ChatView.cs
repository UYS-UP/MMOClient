using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatView : BaseView
{
    private Button submitButton;
    private TMP_InputField messageInput;
    private Toggle emojiToggle;
    private RectTransform emojiListRect;
    
    private Dictionary<ChatType, ToggleUI> chatToggles= new Dictionary<ChatType, ToggleUI>();
    private ChatScrollView infiniteScrollView;
    
    private ChatType currentType = ChatType.World;

    private ChatController controller;

    protected override void Awake()
    {
        base.Awake();
        controller = new ChatController(this);
        submitButton = transform.Find("SubmitButton").GetComponent<Button>();
        messageInput = transform.Find("MessageInput").GetComponent<TMP_InputField>();
        emojiToggle = transform.Find("EmojiToggle").GetComponent<Toggle>();
        emojiListRect = transform.Find("EmojiList").GetComponent<RectTransform>();
        
        chatToggles.Add(ChatType.World, transform.Find("ChatMenu/World").GetComponent<ToggleUI>());
        chatToggles.Add(ChatType.Region, transform.Find("ChatMenu/Region").GetComponent<ToggleUI>());
        chatToggles.Add(ChatType.Team, transform.Find("ChatMenu/Team").GetComponent<ToggleUI>());
        chatToggles.Add(ChatType.System, transform.Find("ChatMenu/System").GetComponent<ToggleUI>());
        infiniteScrollView = transform.GetComponentInChildren<ChatScrollView>();
        
        foreach (var kv in chatToggles)
        {
            var type = kv.Key;
            var toggle = kv.Value;

            toggle.OnValueChanged += () => SetActiveChatToggle(type);
        }

        // 默认激活第一个（例如 World）
        SetActiveChatToggle(ChatType.World);
    }
    

    private void SetActiveChatToggle(ChatType type)
    {
        currentType = type;
        var messages = controller.GetChatTypeList(currentType);
        foreach (var kv in chatToggles)
        {
            bool active = kv.Key == type;
            kv.Value.SetActiveState(active, triggerEvent: false);
            if (!active) continue;
            RedPointService.Instance.OnChatTabViewed(type);
            infiniteScrollView.Initialize(messages, BindChatItem);
        }


    }
    
    private void BindChatItem(GameObject item, ChatMessageData data)
    {
        var ui = item.GetComponent<ChatItemUI>();
        ui.UpdateInfo(data.SenderName, data.Content, data.Timestamp);
    }
    

    public void ReceiveChatMessage(ChatMessageData data)
    {
        if (data.Type == currentType)
        {
            infiniteScrollView.AddItem(data);
            return;
        }
        RedPointService.Instance.OnChatMessage(data.Type);
    }
}
