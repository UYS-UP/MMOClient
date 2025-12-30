using System.Collections.Generic;
using MessagePack;

[MessagePackObject]
public class TeamData
{
    [Key(0)] public int TeamId;
    [Key(1)] public string TeamName;
    [Key(2)] public string LeaderPlayerId; // 队长ID
    [Key(3)] public int MaxPlayers;

    // 成员列表 (Key: PlayerId)
    [Key(4)] public Dictionary<string, TeamMember> Members = new Dictionary<string, TeamMember>();

    public TeamData() { }
    public TeamData(int id, string teamName, TeamMember leader, int maxPlayers)
    {
        TeamId = id;
        TeamName = teamName;
        LeaderPlayerId = leader.PlayerId;
        Members[leader.PlayerId] = leader;
        MaxPlayers = maxPlayers;
    }

    public bool IsLeader(string playerId) => LeaderPlayerId == playerId;
    public List<string> GetMemberPlayerIds() => new List<string>(Members.Keys);

}

[MessagePackObject]
public class TeamMember
{
    [Key(0)] public string PlayerId;
    [Key(1)] public string CharacterId;
    [Key(2)] public string Name;
    [Key(3)] public int Level;
}