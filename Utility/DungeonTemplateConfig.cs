using System.Collections.Generic;
using System.Numerics;

public static class DungeonTemplateConfig
{
    public static readonly Dictionary<short, List<string>> regionDungeons = new Dictionary<short, List<string>>
    {
        {
            0, new List<string> {"fuben01"}
        }
    };

    public static readonly Dictionary<string, DungeonTemplate> dungeonTemplates = new Dictionary<string, DungeonTemplate>
    {
        {
            "fuben01", new DungeonTemplate(
                id: "fuben01",
                name : "嚎哭深渊",
                minPlayers: 1,
                maxPlayers: 5,
                minLevel: 0,
                navMeshPath: "D:\\Project\\UnityDemo\\MMORPGServer\\Data\\GameScene_0_fuben01.bin",
                entryPosition: Vector3.Zero,
                regionId: "001",
                monsterPostions: new Dictionary<Vector3, string>
                {
                    {
                        new Vector3(30, 0, 30), "monster_0001"
                    },
                    {
                        new Vector3(35, 0, 35), "monster_0001"
                    }
                },
                bossPostion: new Vector3(50, 0 ,35),
                bossTemplateId: "monster_0001",
                limitTime: 30f
            )
        }
    };



    public static bool TryGetTemplateById(string id, out DungeonTemplate template)
    {
        if (dungeonTemplates.TryGetValue(id, out template)) return true;
        return false;
    }

    public static List<DungeonTemplate> GetTemplates(short regionId)
    {
        List<DungeonTemplate> templates = new List<DungeonTemplate>();
        if (!regionDungeons.TryGetValue(regionId, out var templateIds)) return templates;
        foreach (var id in templateIds)
        {
            if (!dungeonTemplates.TryGetValue(id, out var template)) continue;
            templates.Add(template);
        }

        return templates;
    }
}

public class DungeonTemplate
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int MinLevel { get; set; }
    public string NavMeshPath { get; set; }
    public Vector3 EntryPosition { get; set; }
    public string RegionId { get; set; }
    public Dictionary<Vector3, string> MonsterPostions { get; set; }
    public Vector3 BossPosition { get; set; }
    public string BossTemplateId { get; set; }
    public float LimitTime { get; set; }


    public DungeonTemplate(string id, string name, int minPlayers, int maxPlayers, int minLevel, string navMeshPath, Vector3 entryPosition, string regionId, string bossTemplateId, Vector3 bossPostion, Dictionary<Vector3, string> monsterPostions, float limitTime)
    {
        Id = id;
        Name = name;
        MinPlayers = minPlayers;
        MaxPlayers = maxPlayers;
        MinLevel = minLevel;
        NavMeshPath = navMeshPath;
        EntryPosition = entryPosition;
        RegionId = regionId;
        BossTemplateId = bossTemplateId;
        BossPosition = bossPostion;
        MonsterPostions = monsterPostions;
        LimitTime = limitTime;
    }
}
