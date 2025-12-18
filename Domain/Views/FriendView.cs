
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendView : BaseView
{

    private UnityEngine.RectTransform OperationRect;
    private UnityEngine.UI.Button InviteButton;
    private UnityEngine.UI.Button RemarkButton;
    private UnityEngine.UI.Button ChatButton;
    private UnityEngine.UI.Button DeleteButton;
    private UnityEngine.RectTransform FriendChatFrameRect;
    private UnityEngine.RectTransform HistoryRect;
    private FriendScrollView FriendScrollView;
    private UnityEngine.UI.Toggle AddFriendToggle;
    private UnityEngine.UI.Toggle SearchFriendToggle;
    private UnityEngine.UI.Toggle AddRemarkToggle;
    private TMPro.TMP_InputField ActionInput;
    private UnityEngine.UI.Button EnterButton;
    private UnityEngine.UI.Toggle NotifyToggle;
    private UnityEngine.UI.ToggleGroup ToggleGroup;
    private UnityEngine.RectTransform FriendRequestFrameRect;
    private UnityEngine.RectTransform FriendRequestContentRect;
    private ChatScrollView ChatScrollView;
    private TMPro.TMP_Dropdown GroupDropdown;
    
    private string currentFriendId; // 当前操作的好友ID
    private string currentGroupId; // 当前好友所在的分组ID
    
    // 存储分组ID和下拉选项索引的映射
    private Dictionary<int, string> indexToGroupIdMap = new Dictionary<int, string>();

    private void BindComponent()
    {
        var root = this.transform;

        {
            var t = root.Find("FriendsFrame/Operation");
            OperationRect = t ? t.GetComponent<UnityEngine.RectTransform>() : null;
        }

        {
            var t = root.Find("FriendsFrame/Operation/InviteButton");
            InviteButton = t ? t.GetComponent<UnityEngine.UI.Button>() : null;
        }

        {
            var t = root.Find("FriendsFrame/Operation/RemarkButton");
            RemarkButton = t ? t.GetComponent<UnityEngine.UI.Button>() : null;
        }

        {
            var t = root.Find("FriendsFrame/Operation/ChatButton");
            ChatButton = t ? t.GetComponent<UnityEngine.UI.Button>() : null;
        }

        {
            var t = root.Find("FriendsFrame/Operation/DeleteButton");
            DeleteButton = t ? t.GetComponent<UnityEngine.UI.Button>() : null;
        }

        {
            var t = root.Find("FriendChatFrame");
            FriendChatFrameRect = t ? t.GetComponent<UnityEngine.RectTransform>() : null;
        }

        {
            var t = root.Find("FriendChatFrame/History");
            HistoryRect = t ? t.GetComponent<UnityEngine.RectTransform>() : null;
        }

        {
            var t = root.Find("FriendsFrame/FriendScrollView");
            FriendScrollView = t ? t.GetComponent<FriendScrollView>() : null;
        }

        {
            var t = root.Find("FriendsFrame/ToggleGroup/AddFriendToggle");
            AddFriendToggle = t ? t.GetComponent<UnityEngine.UI.Toggle>() : null;
        }

        {
            var t = root.Find("FriendsFrame/ToggleGroup/SearchFriendToggle");
            SearchFriendToggle = t ? t.GetComponent<UnityEngine.UI.Toggle>() : null;
        }

        {
            var t = root.Find("FriendsFrame/ToggleGroup/AddRemarkToggle");
            AddRemarkToggle = t ? t.GetComponent<UnityEngine.UI.Toggle>() : null;
        }

        {
            var t = root.Find("FriendsFrame/ActionInput");
            ActionInput = t ? t.GetComponent<TMPro.TMP_InputField>() : null;
        }

        {
            var t = root.Find("FriendsFrame/EnterButton");
            EnterButton = t ? t.GetComponent<UnityEngine.UI.Button>() : null;
        }

        {
            var t = root.Find("FriendsFrame/ToggleGroup/NotifyToggle");
            NotifyToggle = t ? t.GetComponent<UnityEngine.UI.Toggle>() : null;
        }

        {
            var t = root.Find("FriendsFrame/ToggleGroup");
            ToggleGroup = t ? t.GetComponent<UnityEngine.UI.ToggleGroup>() : null;
        }

        {
            var t = root.Find("FriendRequestFrame");
            FriendRequestFrameRect = t ? t.GetComponent<UnityEngine.RectTransform>() : null;
        }

        {
            var t = root.Find("FriendRequestFrame/Content");
            FriendRequestContentRect = t ? t.GetComponent<UnityEngine.RectTransform>() : null;
        }

        {
            var t = root.Find("FriendChatFrame/Chat/Middle/ChatScrollView");
            ChatScrollView = t ? t.GetComponent<ChatScrollView>() : null;
        }

        {
            var t = root.Find("FriendsFrame/Operation/GroupDropdown");
            GroupDropdown = t ? t.GetComponent<TMPro.TMP_Dropdown>() : null;
        }

    }




    
    protected override void Awake()
    {
        base.Awake();
        BindComponent();
        EnterButton.onClick.AddListener(OnEnterButtonClick);
        AddFriendToggle.onValueChanged.AddListener((isOn) =>
        {
            if(!isOn) return;
            AlterActionInputPlaceholder("请输入玩家角色名称......");
        });
        AddRemarkToggle.onValueChanged.AddListener(isOn =>
        {
            if(!isOn) return;
            AlterActionInputPlaceholder("请输入分组名称......");
        });
        SearchFriendToggle.onValueChanged.AddListener(isOn =>
        {
            if(!isOn) return;
            AlterActionInputPlaceholder("请输入好友名称......");
        });
        
        GroupDropdown.onValueChanged.AddListener(OnGroupSelected);
        
        FriendModel.Instance.OnFriendGroupReceived += OnFriendGroupReceived;
        FriendModel.Instance.OnFriendRequestReceived += OnFriendRequestReceived;
        FriendModel.Instance.OnFriendReceived += OnFriendReceived;
        
        FriendModel.Instance.OnAddFriendResponseReceived += OnAddFriendResponseReceived;
        
        FriendRequestFrameRect.gameObject.SetActive(false);
    }

    private void Start()
    {
        Initialize();
    }

    private void BindPrivateItemUI(GameObject obj, ChatMessageData data)
    {
        var ui = obj.GetComponent<PrivateChatItemUI>();
        ui.Init(data.Content, data.SenderId == "1");
    }

    private void Initialize()
    {
        foreach (var group in FriendModel.Instance.GetFriendGroups())
        {
            OnFriendGroupReceived(group);
        }

        foreach (var friend in FriendModel.Instance.GetFriends())
        {
            OnFriendReceived(friend);
        }

        foreach (var friendRequest in FriendModel.Instance.GetFriendRequests())
        {
            OnFriendRequestReceived(friendRequest);
        }
        ChatScrollView.Initialize(new List<ChatMessageData>(), BindPrivateItemUI);
    }

    private void OnFriendGroupReceived(NetworkFriendGroupData data)
    {
       
        var node = new TreeNode<ITreePayload>($"FriendGroup_{data.GroupId}",
            new GroupInfo { GroupId = data.GroupId, Name = data.GroupName });
        FriendScrollView.AddRoot(node);
    }

    private void OnFriendRequestReceived(NetworkFriendRequestData data)
    {
        var obj = ResourceService.Instance.LoadResource<GameObject>("Prefabs/UI/HUD/FriendRequestItemUI");
        var friendRequestItemUI = Instantiate(obj, FriendRequestContentRect, false).GetComponent<FriendRequestItemUI>();
        friendRequestItemUI.Init(data.RequestId, data.SenderName, data.Remark);
    }

    private void Update()
    {
        // if (Input.GetKeyDown(KeyCode.V))
        // {
        //     var obj = ResourceService.Instance.LoadResource<GameObject>("Prefabs/UI/HUD/FriendRequestItemUI");
        //     var friendRequestItemUI = Instantiate(obj, FriendRequestContentRect, false).GetComponent<FriendRequestItemUI>();
        //     friendRequestItemUI.Init("12345", "测试", "你好");
        // }
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     var node = new TreeNode<ITreePayload>($"FriendGroup_1",
        //         new GroupInfo { GroupId = "1", Name = "测试" });
        //     FriendScrollView.AddRoot(node);
        //     
        // }
        //
        // if (Input.GetKeyDown(KeyCode.Z))
        // {
        //     var root = FriendScrollView.GetRoot($"FriendGroup_1");
        //     FriendScrollView.AddChildren(root, new TreeNode<ITreePayload>($"FriendGroup_1_1", 
        //         new FriendInfo {Avatar = null, FriendId = "1", DisplayName = "小明", Online = false}));
        // }
        //
        // if (Input.GetKeyDown(KeyCode.J))
        // {
        //     ChatScrollView.AddItem(new ChatMessageData
        //     {
        //         Content = "哈哈哈哈哈哈哈哈哈",
        //         SenderId = "1",
        //         SenderName = "张三",
        //         TargetId = "ABC",
        //         Timestamp = DateTime.Now,
        //         Type = ChatType.Private
        //     });
        // }
        //
        // if (Input.GetKeyDown(KeyCode.F))
        // {
        //     ChatScrollView.AddItem(new ChatMessageData
        //     {
        //         Content = "呵呵呵呵呵呵",
        //         SenderId = "2",
        //         SenderName = "李四",
        //         TargetId = "ABC",
        //         Timestamp = DateTime.Now,
        //         Type = ChatType.Private
        //     });
        // }
    }

    private void OnFriendReceived(NetworkFriendData data)
    {
        var root = FriendScrollView.GetRoot($"FriendGroup_{data.GroupId}");
        FriendScrollView.AddChildren(root, new TreeNode<ITreePayload>($"FriendGroup_{data.GroupId}_{data.CharacterId}", 
            new FriendInfo {Avatar = null, FriendId = data.CharacterId, DisplayName = data.CharacterName, Online = false}));
    }

    private void OnAddFriendResponseReceived(string data)
    {
        
    }

    private void AlterActionInputPlaceholder(string message)
    {
        var placeholder = ActionInput.placeholder;
        TMP_Text tmpText = placeholder as TMP_Text;
        if (tmpText != null)
        {
            tmpText.text = message;
        }
    }

    public void ShowFriendOperation()
    {
        GroupDropdown.ClearOptions();
        indexToGroupIdMap.Clear();
        int index = 0;
        foreach (var group in FriendModel.Instance.GetFriendGroups())
        {
            var option = new TMP_Dropdown.OptionData(group.GroupName);
            GroupDropdown.options.Add(option);
            
            // 建立索引到分组ID的映射
            indexToGroupIdMap[index] = group.GroupId;
            
            // 如果是当前分组，设置选中状态
            if (group.GroupId == currentGroupId)
            {
                GroupDropdown.value = index;
            }
            
            index++;
        }
        
        // 刷新显示的值
        GroupDropdown.RefreshShownValue();
        
    }
    
    
    private void OnGroupSelected(int selectedIndex)
    {
        
        if (string.IsNullOrEmpty(currentFriendId))
        {
            Debug.LogWarning("未设置当前好友ID");
            return;
        }
        
        // 获取选中的分组ID
        if (indexToGroupIdMap.TryGetValue(selectedIndex, out string targetGroupId))
        {
            if(currentGroupId == targetGroupId) return;
            
            // 调用修改分组的方法
            FriendModel.Instance.AlterFriendGroup(currentFriendId, targetGroupId);
            
            // 更新当前分组ID
            currentGroupId = targetGroupId;
        }
    }


    private void OnEnterButtonClick()
    {
        if (ToggleGroup.GetFirstActiveToggle() == AddFriendToggle)
        {
            // 触发添加好友
            FriendModel.Instance.AddFriend(ActionInput.text);
        }else if (ToggleGroup.GetFirstActiveToggle() == SearchFriendToggle)
        {
            // 触发搜索好友
        }else if (ToggleGroup.GetFirstActiveToggle() == AddRemarkToggle)
        {
            // 触发添加分组
        }
    }

    private void OnDestroy()
    {
        FriendModel.Instance.OnFriendGroupReceived -= OnFriendGroupReceived;
        FriendModel.Instance.OnFriendRequestReceived -= OnFriendRequestReceived;
        FriendModel.Instance.OnFriendReceived -= OnFriendReceived;
        
        FriendModel.Instance.OnAddFriendResponseReceived -= OnAddFriendResponseReceived;
        GroupDropdown.onValueChanged.RemoveListener(OnGroupSelected);
    }
    
}


