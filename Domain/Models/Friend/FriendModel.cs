
using System;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;

[MessagePackObject]
public class NetworkFriendData
{
    [Key(0)] public string CharacterId;
    [Key(1)] public string CharacterName;
    [Key(2)] public ProfessionType Type;
    [Key(3)] public string Avatar;
    [Key(4)] public int Level;
    [Key(5)] public string GroupId;
}

[MessagePackObject]
public class NetworkFriendRequestData
{
    [Key(0)] public string RequestId;
    [Key(1)] public string SenderName;
    [Key(2)] public string Remark;
}

[MessagePackObject]
public class NetworkFriendGroupData
{
    [Key(0)] public string GroupId;
    [Key(1)] public string GroupName;
}

public class FriendModel : Singleton<FriendModel>, IDisposable
{
    private Dictionary<string, NetworkFriendData> friends = new Dictionary<string, NetworkFriendData>();
    private Dictionary<string, NetworkFriendGroupData>  friendGroups = new Dictionary<string, NetworkFriendGroupData>();
    private Dictionary<string, NetworkFriendRequestData> friendRequests = new Dictionary<string, NetworkFriendRequestData>();
    
    public event Action<NetworkFriendRequestData> OnFriendRequestReceived;
    public event Action<NetworkFriendGroupData> OnFriendGroupReceived;
    public event Action<NetworkFriendData> OnFriendReceived;
    
    public event Action<string> OnAddFriendResponseReceived;

    public IEnumerable<NetworkFriendData> GetFriends() => friends.Values;
    public IEnumerable<NetworkFriendGroupData> GetFriendGroups() => friendGroups.Values;
    public IEnumerable<NetworkFriendRequestData> GetFriendRequests() => friendRequests.Values;

    public FriendModel()
    {
        Debug.Log("FriendModel created");
        ProtocolRegister.Instance.OnFriendListSyncEvent += OnFriendListSyncEvent;
        ProtocolRegister.Instance.OnAddFriendGroupEvent += OnAddFriendGroupEvent;
        ProtocolRegister.Instance.OnAddFriendEvent += OnAddFriendEvent;
        ProtocolRegister.Instance.OnAddFriendRequestEvent += OnAddFriendRequestEvent;
        ProtocolRegister.Instance.OnHandleFriendRequestEvent += OnHandleFriendRequestEvent;
    }

    private void OnFriendListSyncEvent(ServerFriendListSync data)
    {
        Debug.Log("Friend list sync event");
        foreach (var group in data.Groups)
        {
            friendGroups[group.GroupId] = group;
            OnFriendGroupReceived?.Invoke(group);
        }

        foreach (var request in data.Requests)
        {
            friendRequests[request.RequestId] = request;
            OnFriendRequestReceived?.Invoke(request);
        }

        foreach (var friend in data.Friends)
        {
            friends[friend.GroupId] = friend;
            OnFriendReceived?.Invoke(friend);
        }
    }
    private void OnAddFriendRequestEvent(NetworkFriendRequestData data)
    {
        friendRequests[data.RequestId] = data;
        OnFriendRequestReceived?.Invoke(data);
    }

    private void OnAddFriendEvent(ServerAddFriend data)
    {
        OnAddFriendResponseReceived?.Invoke(data.Message);
    }

    private void OnAddFriendGroupEvent(NetworkFriendGroupData data)
    {
        Debug.Log("OnAddFriendGroupEvent: " + data.GroupName);
        friendGroups[data.GroupId] = data;
        OnFriendGroupReceived?.Invoke(data);
    }
    
    private void OnHandleFriendRequestEvent(NetworkFriendData data)
    {
        friends[data.CharacterId] = data;
        OnFriendReceived?.Invoke(data);
    }

    

    public void AddFriend(string characterName)
    {
        GameClient.Instance.Send(Protocol.AddFriend, new ClientAddFriend { CharacterName = characterName });
    }

    public void HandleAddFriendRequest(string requestId, bool isAccept)
    {
        GameClient.Instance.Send(Protocol.HandleFriendRequest, new ClientHandleAddFriendRequest {RequestId = requestId, IsAccept = isAccept});
    }

    public void AlterFriendRemark(string characterId, string remark)
    {
        
    }

    public void AlterFriendGroup(string friendId, string groupId)
    {
        
    }

    public void InviteFriend(string characterId)
    {
        
    }

    public void DeleteFriend(string characterId)
    {
        
    }
    
    
    public void Dispose()
    {
      
    }
}
