using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityPrefabConfig", menuName = "Config/EntityPrefabConfig")]
public class EntityPrefabConfig : ScriptableObject
{
    [System.Serializable]
    public struct PrefabEntry
    {
        public EntityType Type;
        public bool IsLocal;
        public GameObject Prefab;
    }

    public List<PrefabEntry> Prefabs;

    public GameObject GetPrefab(EntityType type, bool isLocal)
    {
        return Prefabs.Find(entry => entry.Type == type && entry.IsLocal == isLocal).Prefab;
    }
}