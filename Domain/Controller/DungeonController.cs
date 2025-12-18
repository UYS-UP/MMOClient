
using System;
using System.Collections.Generic;

public class DungeonController : IDisposable
{
    private readonly TeamModel teamModel;
    private readonly DungeonView dungeonView;


    public DungeonController(DungeonView dungeonView)
    {
        teamModel = GameContext.Instance.Get<TeamModel>();
        this.dungeonView = dungeonView;
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        teamModel.OnTeamCreated += OnTeamCreated;
        teamModel.OnTeamUpdated += OnTeamUpdated;
    }

    private void UnregisterEvents()
    {
        teamModel.OnTeamCreated -= OnTeamCreated;
        teamModel.OnTeamUpdated -= OnTeamUpdated;
    }

    #region TeamModel Events

    private void OnTeamCreated(List<TeamMember> members)
    {
        dungeonView.CreateTeam(members);
    }

    private void OnTeamUpdated()
    {
        if(!teamModel.TryGetTeam(out var team)) return;
        dungeonView.UpdateTeam(team.MaxPlayers, team.TeamMembers);
    }

    #endregion

    public void StartDungeon()
    {
        if(!teamModel.TryGetTeam(out var team)) return;
        GameClient.Instance.Send(Protocol.StartDungeon, team.TeamId);
    }

    public void InviteRegion()
    {
        if(!teamModel.TryGetTeam(out var team)) return;
        GameClient.Instance.Send(Protocol.InvitePlayer, team.TeamId);
    }
    
    public void Dispose()
    {
        UnregisterEvents();
    }
}
