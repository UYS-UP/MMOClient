using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RedPointNode
{
    public string Path;
    public RedPointNode Parent;
    public Dictionary<string, RedPointNode> Children = new Dictionary<string, RedPointNode>();
    
    
    public int SelfCount { get; private set; } // 未读/待处理数量
    public int TotalCount {get; private set;} // 总的计数(自己的数量+所有子节点的数量)

    public event Action<RedPointNode> OnChanged;

    public RedPointNode(string path, RedPointNode parent)
    {
        Path = path;
        Parent = parent;
    }

    public void SetSelf(int value)
    {
        value = Mathf.Max(0, value);
        if(value == SelfCount) return;
        int delta = value - SelfCount;
        SelfCount = value;
        Propagate(delta);
    }

    public void Add(int delta = 1)
    {
        if (delta == 0) return;
        SelfCount = Mathf.Max(0, SelfCount + delta);
        Propagate(delta);
    }
    
    public void Clear()
    {
        if (SelfCount == 0) return;
        int delta = -SelfCount;
        SelfCount = 0;
        Propagate(delta);
    }

    private void Propagate(int delta)
    {
        if(delta == 0) return;
        var node = this;
        // 自底向上更新
        while (node != null)
        {
            node.TotalCount = Mathf.Max(0, node.TotalCount + delta);
            node.OnChanged?.Invoke(node);
            node = node.Parent;
        }
    }
}