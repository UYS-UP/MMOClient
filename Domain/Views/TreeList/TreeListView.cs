using System;
using System.Collections.Generic;
using UnityEngine;

public class TreeListView : InfiniteScrollView<TreeNode<ITreePayload>>
{
    private readonly List<TreeNode<ITreePayload>> roots = new List<TreeNode<ITreePayload>>();
    private readonly List<TreeNode<ITreePayload>> visibles = new List<TreeNode<ITreePayload>>();
    private Func<ITreePayload, string> titleGetter = null;
    public IReadOnlyList<TreeNode<ITreePayload>> Visibles => visibles;

    public TreeNode<ITreePayload> GetRoot(string rootId)
    {
        foreach (var node in roots)
        {
            if (node.Id != rootId) continue;
            return node;
        }

        return null;
    }
    
    public void InitializeTree()
    {
        this.titleGetter = titleGetter ?? (s => s.ToString());
        RebuildVisible();
        base.Initialize(visibles, OnBindTreeItem);
    }

    public void AddRoot(TreeNode<ITreePayload> root)
    {
        roots.Add(root);
        RebuildVisible();
        RefreshFromVisible();
    }
    
    public void AddChildren(TreeNode<ITreePayload> parent, List<TreeNode<ITreePayload>> childrens, bool expandAfterAdd = true)
    {
        if (parent == null) return;
        foreach (var c in childrens)
        {
            parent.AddChild(c);
        }

        if (expandAfterAdd)
            parent.IsExpanded = true;

        RebuildVisible();
        RefreshFromVisible();
    }
    
    public void AddChildren(TreeNode<ITreePayload> parent, TreeNode<ITreePayload> children, bool expandAfterAdd = true)
    {
        if (parent == null) return;
        parent.AddChild(children);
        if (expandAfterAdd)
            parent.IsExpanded = true;

        RebuildVisible();
        RefreshFromVisible();
    }
    
    private void RebuildVisible()
    {
        visibles.Clear();
        foreach (var root in roots)
        {
            root.Depth = 0;
            Flatten(root, visibles);
        }
    }
    
    private void Flatten(TreeNode<ITreePayload> node, List<TreeNode<ITreePayload>> list)
    {
        list.Add(node);
        if (!node.IsExpanded) return;

        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            child.Parent = node;
            child.Depth  = node.Depth + 1;
            Flatten(child, list);
        }
    }
    
    private void OnBindTreeItem(GameObject go, TreeNode<ITreePayload> node)
    {
        if (node.Data.Kind == NodeKind.Friend)
        {
            var view = go.GetComponent<FriendItemUI>();
            if (view == null)
            {
                Debug.LogError("TreeListItemView missing on itemPrefab.");
                return;
            }

            string title = titleGetter != null ? titleGetter(node.Data) : (node.Data?.ToString() ?? "(null)");

            // 通过回调将点击 -> 切换展开
            view.Bind(node);
        }
        else
        {
            var view = go.GetComponent<GroupItemUI>();
            if (view == null)
            {
                Debug.LogError("TreeListItemView missing on itemPrefab.");
                return;
            }
            
            // 通过回调将点击 -> 切换展开
            view.Bind(node, () =>
            {
                int idx = visibles.IndexOf(node);
                ToggleAtVisibleIndex(idx);
            });
        }

    }
    
    private void RefreshFromVisible()
    {
        dataList.Clear();
        dataList.AddRange(visibles);
        FullRebuild(); 
    }
    
    private void ToggleAtVisibleIndex(int visibleIndex)
    {
        if (visibleIndex < 0 || visibleIndex >= visibles.Count) return;
        var node = visibles[visibleIndex];

        if (!node.HasChildren) return;

        node.IsExpanded = !node.IsExpanded;
        
        RebuildVisible();
        RefreshFromVisible();
    }
}