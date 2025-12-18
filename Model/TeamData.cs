using System.Collections.Generic;
using MessagePack;

public enum TeamType
{
    Dungeon, // 副本队伍
    Pvp,    // PVP 竞技场队伍
    World   // 野外组队
}

[MessagePackObject]
[Union(0, typeof(DungeonTeamData))]
public abstract class TeamBaseData
{
    [Key(0)] public TeamMember Leader;
    [Key(1)] public List<TeamMember> TeamMembers;
    [Key(2)] public TeamType TeamType;
    [Key(3)] public string TeamName;
    [Key(4)] public int TeamId;
    [Key(5)] public int MaxPlayers;
    [Key(6)] public int MinPlayers;
}

[MessagePackObject]
public class TeamMember
{
    [Key(0)] public string PlayerId;
    [Key(1)] public string CharacterId;
    [Key(2)] public string Name;
    [Key(3)] public int Level;
}

[MessagePackObject]
public class DungeonTeamData : TeamBaseData
{
    [Key(7)] public string DungeonTemplateId;
}