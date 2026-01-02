using System;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;

public enum SlotContainerType
{
    None,
    Inventory,
    Equipment,
    QuickBar,
}

[MessagePackObject, Serializable]
public struct SlotKey : IEquatable<SlotKey>
{
    [Key(0)] public SlotContainerType Container;
    [Key(1)] public int Index;

    public SlotKey(SlotContainerType container, int index)
    {
        Container = container;
        Index = index;
    }
    
    public override bool Equals(object obj)
    {
        return obj is SlotKey slot && Equals(slot);
    }

    public override string ToString()
    {
        return $"{Container}_{Index}";
    }

    public bool Equals(SlotKey other)
    {
        return Container == other.Container && Index == other.Index;
    }
    
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + Container.GetHashCode();
            hash = hash * 23 + Index.GetHashCode();
            return hash;
        }
    }
    
    public static bool operator ==(SlotKey left, SlotKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SlotKey left, SlotKey right)
    {
        return !(left == right);
    }
}

public class StorageModel :  IDisposable
{
    private enum ApplyMode { Optimistic, WaitServerAck, ServerOnlySendsOnFailure }
    private readonly ApplyMode Mode = ApplyMode.Optimistic;
    private readonly Dictionary<SlotKey, ItemData> storage = new Dictionary<SlotKey, ItemData>();
    
    public int MaxSize { get; private set; } = -1;
    public int MaxOccupiedSlot { get; private set; } = -1;
    
    public bool IsFullyLoaded { get; private set; } = false;
    
    public event Action<SlotKey> OnSlotChanged;
    public event Action<IReadOnlyList<SlotKey>> OnSlotsChanged;
    public event Action<int> OnInventoryResized;
    public event Action OnItemAcquired;
    
    private int nextReqId = 1;
    
    private struct PendingSwap
    {
        public int ReqId;
        public SlotKey From;
        public SlotKey To;
        public ItemData PrevA, PrevB;
    }

    private readonly Dictionary<int, PendingSwap> pendings = new();
    
    // 修改：批量加载大小，可以更大
    public readonly int BATCH_SIZE = 100;
    private int currentBatchStart = 0;
    private bool isLoading = false;

    public StorageModel()
    {
        GameClient.Instance.RegisterHandler(Protocol.SC_SwapStorageSlot, OnSwapInventorySlot);
        GameClient.Instance.RegisterHandler(Protocol.SC_QueryInventory, OnQueryInventory);
        GameClient.Instance.RegisterHandler(Protocol.SC_AddInventoryItem, OnAddInventoryItem);
    }

    public void SetMaxSize(int maxSize)
    {
        if (maxSize != MaxSize)
        {
            MaxSize = maxSize;
            Debug.Log("Max size changed to: " + MaxSize);
            OnInventoryResized?.Invoke(MaxSize);
        }
    }

    // 新增：开始预加载整个背包
    public void PreloadInventory()
    {
        if (IsFullyLoaded || isLoading) return;
        
        currentBatchStart = 0;
        LoadNextBatch();
    }
    
    // 新增：加载下一批数据
    private void LoadNextBatch()
    {
        if (isLoading) return;
        
        if (MaxSize == -1)
        {
            RequestInventoryData(0, BATCH_SIZE);
            return;
        }
        
        if (currentBatchStart >= MaxOccupiedSlot)
        {
            IsFullyLoaded = true;
            Debug.Log("背包数据加载完成");
            return;
        }
        
        int endSlot = Mathf.Min(currentBatchStart + BATCH_SIZE, MaxSize);
        RequestInventoryData(currentBatchStart, endSlot);
    }

    public void UpsertRange(Dictionary<SlotKey, ItemData> data)
    {
        if (data == null) return;
        var updatedSlots = new List<SlotKey>();
        foreach (var kv in data)
        {
            SetOrRemove(kv.Key, kv.Value);
            updatedSlots.Add(kv.Key);
        }
        
        OnSlotsChanged?.Invoke(updatedSlots);
    }

    public void UpsertSlot(SlotKey slot, ItemData data)
    {
        if (data == null) return;
        SetOrRemove(slot, data);
    }

    private void OnAddInventoryItem(GamePacket packet)
    {
        var data = packet.DeSerializePayload<ServerAddItem>();
        if (data == null) return;
        foreach (var kv in data.Items)
        {
            SetOrRemove(kv.Key, kv.Value);
            OnItemAcquired?.Invoke();
        }
    }

    public bool TryGetItem(SlotKey slot, out ItemData item)
    {
        return storage.TryGetValue(slot, out item);
    }

    public void RequestSwap(SlotKey from, SlotKey to)
    {
        if (from == to) return;
        
        // 确保两个槽位都存在（可能为空）
        storage.TryAdd(from, null);
        storage.TryAdd(to, null);
        
        var reqId = nextReqId++;
        var a = storage[from];
        var b = storage[to];

        pendings[reqId] = new PendingSwap { ReqId = reqId, From = from, To = to, PrevA = a, PrevB = b};

        if (Mode == ApplyMode.Optimistic || Mode == ApplyMode.ServerOnlySendsOnFailure)
        {
            ExchangeSlotsLocal(from, to);
        }

        var payload = new ClientSwapStorageSlot
        {
            Slot1 = from,
            Slot2 = to,
        };
        GameClient.Instance.Send(Protocol.CS_SwapStorageSlot, payload);
        
        OnSlotChanged?.Invoke(from);
        OnSlotChanged?.Invoke(to);
    }
    
    
    public void RequestInventoryData(int startSlot, int endSlot)
    {
        if (isLoading) return;

        int maxSize = Mathf.Max(MaxSize, endSlot);
        startSlot = Mathf.Clamp(startSlot, 0, maxSize);
        endSlot = Mathf.Clamp(endSlot, 0, maxSize);

        if (startSlot >= endSlot) return;

        isLoading = true;
        
        Debug.Log($"请求背包数据: {startSlot} - {endSlot}");

        GameClient.Instance.Send(Protocol.CS_QueryInventory, new ClientQueryInventory
        {
            StartSlot = startSlot,
            EndSlot = endSlot,
        });
    }

    private void OnSwapInventorySlot(GamePacket packet)
    {
        var data = packet.DeSerializePayload<ServerSwapStorageSlotResponse>();
        if (!pendings.TryGetValue(data.ReqId, out var p)) return;
        if (!data.Success)
        {
            Rollback(data.ReqId);
            pendings.Remove(data.ReqId);
            return;
        }
        
        if (Mode == ApplyMode.WaitServerAck)
        {
            ExchangeSlotsLocal(p.From, p.To);
        }
        
        if (data.Item1 != null) SetOrRemove(p.From, data.Item1);
        if (data.Item2 != null) SetOrRemove(p.To, data.Item2);

        pendings.Remove(data.ReqId);
    }
    
    private void OnQueryInventory(GamePacket packet)
    {
        var data = packet.DeSerializePayload<ServerQueryInventory>();
        SetMaxSize(data.MaxSize);
        UpsertRange(data.Data);
        MaxOccupiedSlot = data.MaxOccupiedSlot;
        isLoading = false;
        
        // 继续加载下一批
        currentBatchStart += BATCH_SIZE;
        LoadNextBatch();
    }

    private void ExchangeSlotsLocal(SlotKey a, SlotKey b)
    {
        storage.TryGetValue(a, out var A);
        storage.TryGetValue(b, out var B);
        
        SetOrRemove(a, B);
        SetOrRemove(b, A);
    }
    
    private void SetOrRemove(SlotKey slot, ItemData data)
    {
        if (data == null) storage.Remove(slot);
        else storage[slot] = data;

        OnSlotChanged?.Invoke(slot);
    }
    
    private void Rollback(int reqId)
    {
        if (!pendings.TryGetValue(reqId, out var p)) return;
        SetOrRemove(p.From, p.PrevA);
        SetOrRemove(p.To, p.PrevB);
    }

    public List<SlotKey> GetFilteredSlots(SlotContainerType container, string searchName, QualityType qualityFilter,
        ItemType typeFilter)
    {
        var matchedSlots = new List<SlotKey>();
        var emptySlots = new List<SlotKey>();
        int limit = MaxSize > 0 ? MaxSize : 0;
        
        bool isDefaultMode = string.IsNullOrEmpty(searchName) 
                             && qualityFilter == QualityType.None 
                             && typeFilter == ItemType.None;

        if (isDefaultMode)
        {
            for (int i = 0; i < limit; i++)
            {
                matchedSlots.Add(new SlotKey(container, i));
            }
            return matchedSlots;
        }
        

        for (int i = 0; i < limit; i++)
        {
            var key = new SlotKey(container, i);
            
            // 检查该槽位是否有物品
            if (storage.TryGetValue(key, out var item) && item != null)
            {
                // --- 有物品，进行筛选 ---
                // 1. 名称筛选
                bool isMatch = !(!string.IsNullOrEmpty(searchName) && 
                                 !item.ItemName.Contains(searchName));

  
                
                // 2. 品质筛选
                if (isMatch && qualityFilter != QualityType.None && item.QuantityType != qualityFilter) 
                {
                    isMatch = false;
                }
                
                // 3. 类型筛选
                if (isMatch && typeFilter != ItemType.None && item.ItemType != typeFilter) 
                {
                    isMatch = false;
                }

                if (isMatch)
                {
                    matchedSlots.Add(key);
                }
            }
            else
            {
                emptySlots.Add(key);
            }
        }
        matchedSlots.AddRange(emptySlots);
        return matchedSlots;
    }

    public void Dispose()
    {
        storage.Clear();
    }
}