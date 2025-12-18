

using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class TreeNode<T>
{
    public string Id;
    public T Data;

    public TreeNode<T> Parent;
    public readonly List<TreeNode<T>> Children = new List<TreeNode<T>>();

    public bool IsExpanded;
    public int Depth;
    public bool HasChildren => Children.Count > 0;

    public TreeNode(string id, T data, bool isExpanded = false)
    {
        Id = id;
        Data = data;
        IsExpanded = isExpanded;
    }

    public void AddChild(TreeNode<T> child)
    {
        child.Parent = this;
        child.Depth = this.Depth + 1;
        Children.Add(child);
    }
    
    public bool RemoveChild(string childId)
    {
        var child = Children.FirstOrDefault(c => c.Id == childId);
        if (child != null)
        {
            Children.Remove(child);
            return true;
        }
        return false;
    }

    // Find a child by ID
    public TreeNode<T> FindChild(string childId)
    {
        return Children.FirstOrDefault(c => c.Id == childId);
    }
    
    public void SetData(T newData)
    {
        Data = newData;
    }
}