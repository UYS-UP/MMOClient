using System;
using System.Collections.Generic;

public class MinHeap<T>
{
    private readonly List<T> data = new List<T>(256);
    private readonly Comparison<T> cmp;
    public int Count => data.Count;
    public MinHeap(Comparison<T> cmp) { this.cmp = cmp; }
    public void Push(T item)
    {
        data.Add(item);
        SiftUp(data.Count - 1);
    }
    public bool TryPop(out T item)
    {
        if(data.Count == 0)
        {
            item = default;
            return false;
        }
    
        item = data[0];
        int last = data.Count - 1;
        data[0] = data[last];
        data.RemoveAt(last);
        if (data.Count > 0) SiftDown(0);
        return true;
    }

    public T Pop()
    {
        var root = data[0];
        int last = data.Count - 1;
        data[0] = data[last];
        data.RemoveAt(last);
        if (data.Count > 0) SiftDown(0);
        return root;
    }

    public bool Peek(out T item)
    {
        if (data.Count == 0)
        {
            item = default;
            return false;
        }
        item = data[0];
        return true;
    }
    
    private void SiftUp(int i)
    {
        while (i > 0)
        {
            int p = i - 1 >> 1;
            if (cmp(data[i], data[p]) >= 0) break;
            (data[i], data[p]) = (data[p], data[i]);
            i = p;
        }
    }
    private void SiftDown(int i)
    {
        int n = data.Count;
        while (true)
        {
            int l = (i << 1) + 1, r = l + 1, m = i;
            if (l < n && cmp(data[l], data[m]) < 0) m = l;
            if (r < n && cmp(data[r], data[m]) < 0) m = r;
            if (m == i) break;
            (data[i], data[m]) = (data[m], data[i]);
            i = m;
        }
    }

    public void Clear()
    {
        data.Clear();
    }
}