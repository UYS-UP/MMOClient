using System;
using System.Collections.Generic;
using UnityEngine;

public class RedPointService : SingletonMono<RedPointService>
{
    private RedPointNode root;
    private readonly Dictionary<string, RedPointNode> nodes = new Dictionary<string, RedPointNode>();
    
    private const string PREFS_KEY = "REDPOINT_SNAPSHOT_V1";
    
    [Serializable]
    private struct SnapShot
    {
        public List<Item> items;
    }

    [Serializable]
    private struct Item
    {
        public string path;
        public int self;
    }

    protected override void Awake()
    {
        base.Awake();
        root = GetOrCreateNode("");
        Load();
    }
    public void Add(string path, int delta = 1) => GetOrCreateNode(path).Add(delta);
    public void Set(string path, int value) => GetOrCreateNode(path).SetSelf(value);
    public void Clear(string path) => GetOrCreateNode(path).Clear();
    public int GetCount(string path) => GetOrCreateNode(path).TotalCount;
    public bool Has(string path) => GetOrCreateNode(path).TotalCount > 0;
    
    public IDisposable Subscribe(string path, Action<RedPointNode> onChanged)
    {
        var node = GetOrCreateNode(path);
        node.OnChanged += onChanged;
        // 立即推一次，便于 UI 初始同步
       
        onChanged?.Invoke(node);
        return new Unsub(node, onChanged);
    }
    
    public void OnMailArrived(int count = 1) => Add("Mail", count);
    public void OnChatMessage(ChatType type, int count = 1)
        => Add($"Chat/{type}", count);
    public void OnQuestCompleted(int count = 1)
        => Add("Quest/Completed", count);
    
    public void OnMailViewed() => Clear("Mail");
    public void OnChatTabViewed(ChatType type) => Clear($"Chat/{type}");
    public void OnAllChatViewed() => Clear("Chat");     // 清整个 chat 树
    public void OnQuestTabViewed() => Clear("Quest/Completed");

    private RedPointNode GetOrCreateNode(string path)
    {
        path ??= "";
        if (nodes.TryGetValue(path, out var node)) return node;

        if (root == null)
        {
            root = new RedPointNode("", null);
            nodes[""] = root;
        }

        if (path == "")
            return root;

        string[] segs = path.Split("/", StringSplitOptions.RemoveEmptyEntries);
        string cur = "";
        RedPointNode parent = root;

        foreach (var seg in segs)
        {
            cur = cur.Length == 0 ? seg : $"{cur}/{seg}";
            if (!nodes.TryGetValue(cur, out var child))
            {
                child = new RedPointNode(cur, parent);
                parent.Children[seg] = child;
                nodes[cur] = child;          
            }
            parent = child;
        }

        return parent;
    }

    public void Save()
    {
        var s = new SnapShot { items = new List<Item>(nodes.Count) };
        foreach (var kv  in nodes)
        {
            var n = kv.Value;
            if(n.SelfCount > 0 || n.Path == "")
                s.items.Add(new Item {path = n.Path, self = n.SelfCount});
        }

        var json = JsonUtility.ToJson(s);
        PlayerPrefs.SetString(PREFS_KEY, json);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        if(!PlayerPrefs.HasKey(PREFS_KEY)) return;
        string json = PlayerPrefs.GetString(PREFS_KEY);
        if(string.IsNullOrEmpty(json)) return;
        var s = JsonUtility.FromJson<SnapShot>(json);
        if(s.items == null) return;

        foreach (var kv in nodes)
        {
            if (kv.Value.SelfCount != 0) kv.Value.Clear();
        }
        
        foreach (var it in s.items)
        {
            if (string.IsNullOrEmpty(it.path)) continue;
            Set(it.path, it.self);
        }
    }
    
    void OnApplicationPause(bool pause) { if (pause) Save(); }
    void OnApplicationQuit() { Save(); }
    
    private class Unsub : IDisposable
    {
        RedPointNode n; Action<RedPointNode> cb;
        public Unsub(RedPointNode n, Action<RedPointNode> cb) { this.n = n; this.cb = cb; }
        public void Dispose()
        {
            if (n != null && cb != null) n.OnChanged -= cb;
            n = null; cb = null;
        }
    }
}