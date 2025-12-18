using UnityEngine;

public class FriendScrollView : TreeListView
{
    [SerializeField] private GameObject groupItemPrefab;
    [SerializeField] private GameObject friendItemPrefab;
    
    protected override void Awake()
    {
        base.Awake();
        InitializeTree();
        PoolService.Instance.Preload(groupItemPrefab, warmupCount / 2);
        PoolService.Instance.Preload(friendItemPrefab, warmupCount);
        GetPrefabForItem += (node) => node.Data.Kind == NodeKind.Friend ? friendItemPrefab : groupItemPrefab;;
    }
}
