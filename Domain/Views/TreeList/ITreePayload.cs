using UnityEngine;

public enum NodeKind { Group, Friend }

public interface ITreePayload
{
    NodeKind Kind { get; }
    string DisplayText { get; }  // 用于通用显示
}

[System.Serializable]
public class GroupInfo : ITreePayload
{
    public string GroupId;
    public string Name;

    public NodeKind Kind => NodeKind.Group;
    public string DisplayText => Name;
}

[System.Serializable]
public class FriendInfo : ITreePayload
{
    public string FriendId;
    public string DisplayName;
    public Sprite Avatar;     // 可为 null
    public bool Online;

    public NodeKind Kind => NodeKind.Friend;
    public string DisplayText => DisplayName;
}