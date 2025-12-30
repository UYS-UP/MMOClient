using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 队伍系统数据模型
/// 负责管理队伍数据和队伍相关业务逻辑
/// </summary>
public class TeamModel : IDisposable
{
    private TeamData teamData;
    private int pendingInviteTeamId = -1;
    private string pendingInviteMessage;
    
    public event Action<string> OnInviteReceived;
    public event Action OnTeamJoined;
    public event Action OnTeamUpdated;

    public event Action<List<TeamMember>> OnTeamCreated;

    /// <summary>
    /// 获取当前队伍
    /// </summary>
    public TeamData CurrentTeamData => teamData;

    /// <summary>
    /// 是否在队伍中
    /// </summary>
    public bool IsInTeam => teamData != null;

    public TeamModel()
    {

    }
    

    /// <summary>
    /// 尝试获取队伍
    /// </summary>
    public bool TryGetTeam(out TeamData value)
    {
        value = teamData;
        return teamData != null;
    }

    /// <summary>
    /// 接收队伍邀请
    /// </summary>
    private void OnTeamInvitePlayerEvent(ServerDungeonTeamInvite data)
    {
        pendingInviteTeamId = data.TeamId;
        pendingInviteMessage = data.Message;
        OnInviteReceived?.Invoke(pendingInviteMessage);
    }
    
    
    /// <summary>
    /// 清除待处理的邀请
    /// </summary>
    private void ClearPendingInvite()
    {
        pendingInviteTeamId = -1;
        pendingInviteMessage = "";
    }
    
    public void AcceptInvitation()
    {
        if (pendingInviteTeamId != -1)
        {
            GameClient.Instance.Send(Protocol.CS_AcceptInvite, pendingInviteTeamId);
            ClearPendingInvite();
        }
    }
    
    public void RefuseInvitation()
    {
        ClearPendingInvite();
    }
    

    public void Dispose()
    {

    }
}

