using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerEnterSceneEventArgs : EventArgs
{
    public NetworkEntity PlayerEntity;
}

public class RoleSelectedEventArgs : EventArgs
{
    public NetworkCharacter NetworkCharacter;
}

public class EntitySpawnEventArgs : EventArgs
{
    public List<EntityBase> SpawnedEntities;
}

public class EntityDespawnEventArgs : EventArgs
{
    
}

public class MonsterDeathEventArgs : EventArgs
{
    public string EntityId;
    public Vector3 DeathPosition;
}

public class PlayerDeathEventArgs : EventArgs
{
    
}

public class EntityHitEventArgs : EventArgs
{
    public NetworkEntity HitEntity;
    public float HitValue;
}

public class TriggerEnterNpcEventArgs : EventArgs
{
    public int NpcId;
}

public class TriggerExitNpcEventArgs : EventArgs
{
    public int NpcId;
}


public class PlayerPickupItemEventArgs : EventArgs
{
    public string ItemId;
}

public class ChangeDungeonEventArgs : EventArgs
{
    public short RegionId;
    public string DungeonTemplateId;
}




