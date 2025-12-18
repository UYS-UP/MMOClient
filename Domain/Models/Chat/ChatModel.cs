using System;
using System.Collections.Generic;
using MessagePack;

public enum ChatType
{
    World,      // 世界聊天
    Region,     // 区域聊天
    Team,       // 队伍聊天
    System,     // 系统通知
    Private     // 私聊(好友聊天)
}

[MessagePackObject]
public class ChatMessageData
{
    [Key(0)] public string SenderId { get; set; }
    [Key(1)] public string SenderName { get; set; }
    [Key(2)] public ChatType Type { get; set; }
    [Key(3)] public string Content { get; set; }
    [Key(4)] public DateTime Timestamp { get; set; }
    [Key(5)] public string TargetId { get; set; }
}

public class ChatModel : IDisposable
{
    private Dictionary<ChatType, List<ChatMessageData>> Messages = new Dictionary<ChatType, List<ChatMessageData>>
    {
        {
            ChatType.World, new List<ChatMessageData>()
        },
        {
            ChatType.Region, new List<ChatMessageData>()
        },
        {
            ChatType.Team, new List<ChatMessageData>()
        },
        {
            ChatType.System, new List<ChatMessageData>()
        }
    };

    public event Action<ChatMessageData> OnChatMessageReceived;

    public void Test(ChatMessageData data)
    {
        OnChatMessageEvent(data);
    }
    
    public ChatModel()
    {
        // ProtocolRegister.Instance.OnChatMessageEvent += OnChatMessageEvent;
    }

    private void OnChatMessageEvent(ChatMessageData data)
    {
        if (Messages.TryGetValue(data.Type, out var list))
        {
            list.Add(data);
            OnChatMessageReceived?.Invoke(data);
        }
    }


    public void Dispose()
    {
        // ProtocolRegister.Instance.OnChatMessageEvent -= OnChatMessageEvent;
    }

    public List<ChatMessageData> GetChatTypeList(ChatType type)
    {
        if (Messages.TryGetValue(type, out var list))
        {
            return list;
        }
        return new List<ChatMessageData>();
    }
}
