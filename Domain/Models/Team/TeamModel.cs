using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 队伍系统数据模型
/// 负责管理队伍数据和队伍相关业务逻辑
/// </summary>
public class TeamModel : IDisposable
{
    private TeamBaseData teamData;
    private int pendingInviteTeamId = -1;
    private string pendingInviteMessage;
    
    public event Action<string> OnInviteReceived;
    public event Action OnTeamJoined;
    public event Action OnTeamUpdated;

    public event Action<List<TeamMember>> OnTeamCreated;

    /// <summary>
    /// 获取当前队伍
    /// </summary>
    public TeamBaseData CurrentTeamData => teamData;

    /// <summary>
    /// 是否在队伍中
    /// </summary>
    public bool IsInTeam => teamData != null;

    public TeamModel()
    {
        ProtocolRegister.Instance.OnCreateDungeonTeamEvent += OnCreateDungeonTeamEvent;
        ProtocolRegister.Instance.OnTeamInvitePlayerEvent += OnTeamInvitePlayerEvent;
        ProtocolRegister.Instance.OnPlayerEnterTeamEvent += OnPlayerEnterTeamEvent;
    }
    

    /// <summary>
    /// 尝试获取队伍
    /// </summary>
    public bool TryGetTeam(out TeamBaseData value)
    {
        value = teamData;
        return teamData != null;
    }

    /// <summary>
    /// 尝试获取指定类型的队伍
    /// </summary>
    public bool TryGetTeam<T>(out T value) where T : TeamBaseData
    {
        if (teamData is T networkTeam)
        {
            value = networkTeam;
            return value != null;
        }
        value = null;
        return false;
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

    private void OnPlayerEnterTeamEvent(ServerPlayerEnterTeam data)
    {
        if (!data.Success)
        {
            Debug.LogError(data.Message);
            return;
        }

        teamData = data.Team;

        // 自己加入：通知自己进队（用于跳转到组队面板等）
        if (data.Player == PlayerModel.Instance.Player.PlayerId)
        {
            OnTeamJoined?.Invoke();
            return;
        }

        // 其他人加入：只通知队伍更新（用于刷新队员列表）
        OnTeamUpdated?.Invoke();
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
            GameClient.Instance.Send(Protocol.AcceptInvite, pendingInviteTeamId);
            ClearPendingInvite();
        }
    }
    
    public void RefuseInvitation()
    {
        ClearPendingInvite();
    }

    private void OnCreateDungeonTeamEvent(ServerCreateDungeonTeam data)
    {
        Debug.Log(data.Team.TeamId);
        if (data.Success)
        {
            teamData = data.Team;
            OnTeamCreated?.Invoke(teamData.TeamMembers);
            return;
        }
      
        
    }


    public void Dispose()
    {
        ProtocolRegister.Instance.OnCreateDungeonTeamEvent -= OnCreateDungeonTeamEvent;
        ProtocolRegister.Instance.OnTeamInvitePlayerEvent -= OnTeamInvitePlayerEvent;
        ProtocolRegister.Instance.OnPlayerEnterTeamEvent -= OnPlayerEnterTeamEvent;
    }
}

