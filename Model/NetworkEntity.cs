using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;

[MessagePackObject]
[Union(0, typeof(NetworkCharacter))]
[Union(1, typeof(NetworkMonster))]
[Union(2, typeof(NetworkNpc))]
public abstract class NetworkEntity
{
    [Key(0)] public string EntityId { get; set; }
    [Key(1)] public string RegionId { get; set; }
    [Key(2)] public string DungeonId { get; set; }
    [Key(3)] public EntityType EntityType { get; set; }
    [Key(4)] public MotionStateType Motion { get; set; }
    [Key(5)] public ActionStateType Action { get; set; }
    [Key(6)] public Vector3 Position { get; set; }
    [Key(7)] public float Yaw { get; set; }
    [Key(8)] public Vector3 Direction { get; set; }
    [Key(9)] public float Speed { get; set; }


}



[MessagePackObject]
public class NetworkCharacter : NetworkEntity
{
    [Key(20)] public string PlayerId { get; set; }
    [Key(21)] public string CharacterId { get; set; }
    [Key(22)] public string Name { get; set; }
    [Key(23)] public int Level { get; set; }
    [Key(24)] public int MaxHp { get; set; }
    [Key(25)] public int Hp { get; set; }
    [Key(26)] public int MaxMp { get; set; }
    [Key(27)] public int Mp { get; set; }
    [Key(28)] public int MaxEx { get; set; }
    [Key(29)] public int Ex { get; set; }
    [Key(30)] public int Gold { get; set; }
    [Key(31)] public ProfessionType Profession { get; set; }
    [Key(32)] public List<int> Skills { get; set; }
        

}

[MessagePackObject]
public class NetworkMonster : NetworkEntity
{
    [Key(20)] public string MonsterTemplateId { get; set; }
    [Key(21)] public int Level { get; set; }
    [Key(22)] public int MaxHp { get; set; }
    [Key(23)] public int Hp { get; set; }
}

[MessagePackObject]
public class NetworkNpc : NetworkEntity
{
    [Key(20)] public string NpcTemplateId { get; set; }
}


