
using System;
using System.Collections.Generic;

public class ChatController : IDisposable
{
    private readonly ChatModel chatModel;
    private readonly ChatView chatView;

    public ChatController(ChatView chatView)
    {
        chatModel = GameContext.Instance.Get<ChatModel>();
        this.chatView = chatView;
        RegisterEvents();
    }
    
    private void RegisterEvents()
    {
        chatModel.OnChatMessageReceived += OnChatMessageReceived;
    }

    private void UnregisterEvents()
    {
        chatModel.OnChatMessageReceived -= OnChatMessageReceived;
    }

    private void OnChatMessageReceived(ChatMessageData data)
    {
        chatView.ReceiveChatMessage(data);
    }

    public List<ChatMessageData> GetChatTypeList(ChatType type)
    {
        return chatModel.GetChatTypeList(type);
    }
    
    public void Dispose()
    {
        UnregisterEvents();
    }
}
