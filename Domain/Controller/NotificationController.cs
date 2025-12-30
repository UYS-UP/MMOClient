
using System;
using UnityEngine;

public class NotificationController : IDisposable
{
    private readonly TeamModel teamModel;
    private readonly StorageModel storageModel;
    
    private readonly NotificationView notificationView;


    public NotificationController(NotificationView notificationView)
    {
        storageModel = GameContext.Instance.Get<StorageModel>();
        teamModel = GameContext.Instance.Get<TeamModel>();
        this.notificationView = notificationView;
        RegisterEvents();
    }
    
    
    
    public void Dispose()
    {
        UnregisterEvents();
    }
    
    
    private void RegisterEvents()
    {
        teamModel.OnInviteReceived += OnInviteReceived;
        teamModel.OnTeamJoined += OnTeamJoined;
        storageModel.OnItemAcquired += OnItemAcquired;
    }

    private void UnregisterEvents()
    {
        teamModel.OnInviteReceived -= OnInviteReceived;
        teamModel.OnTeamJoined -= OnTeamJoined;
        storageModel.OnItemAcquired -= OnItemAcquired;
    }
    
    
    public void AcceptInvitation()
    {
        teamModel.AcceptInvitation();
    }

    public void RefuseInvitation()
    {
        teamModel.RefuseInvitation();
    }
    
    #region StorageModel Events

    private void OnItemAcquired()
    {
        Sprite icon = ResourceService.Instance.LoadResource<Sprite>("Sprites/Items/Equip/0001");
        notificationView.AcquireItem(icon, "长剑", 1);
    }

    #endregion
    
    #region TeamModel Events

    private void OnInviteReceived(string message)
    {
        notificationView.ReceiveInvite(message);
    }

    private void OnTeamJoined()
    {
        if(!teamModel.TryGetTeam(out var team)) return;
        UIService.Instance.ShowView<DungeonView>((dungeonView) =>
        {
            // dungeonView.TeamJoined(team.MaxPlayers, team.TeamMembers);
        });
    }

    #endregion
}
