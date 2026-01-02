
using System;
using System.Collections.Generic;
using UnityEngine;

public class NavigationController : IDisposable
{
    private readonly StorageModel storageModel;
    private readonly NavigationView navigationView;
    
    public NavigationController(NavigationView navigationView)
    {
        storageModel = GameContext.Instance.Get<StorageModel>();
        this.navigationView = navigationView;
        RegisterEvents();
    }
    
    public void Dispose()
    {
        UnregisterEvents();
    }
    
    private void RegisterEvents()
    {
        storageModel.OnSlotChanged += OnSlotChanged;
    }

    private void UnregisterEvents()
    {
        storageModel.OnSlotChanged -= OnSlotChanged;
    }

    public void ApplyFilter(string searchName, QualityType qualityFilter, ItemType itemFilter)
    {
        var displayList = storageModel.GetFilteredSlots(
            SlotContainerType.Inventory, 
            searchName, 
            qualityFilter, 
            itemFilter
        );
        
        navigationView.UpdateDisplayList(displayList);
        navigationView.ResetScrollPosition();
    }
    
    public List<ItemActionInfo> GetActionsForItem(SlotKey slot, ItemData item)
    {
        var list = new List<ItemActionInfo>();
        list.Add(new ItemActionInfo(item.ItemType == ItemType.Equip ? "装备" : "使用", () => RequestUseItem(slot, item)));
        list.Add(new ItemActionInfo("丢弃", () => RequestDropItem(slot, item), true));
        return list;
    }
    
    private void RequestUseItem(SlotKey slot, ItemData item)
    {
        GameClient.Instance.Send(Protocol.CS_UseItem, new ClientUseItem
        {
            Slot = slot,
            InstanceId = item.InstanceId
        });
    }

    private void RequestDropItem(SlotKey slot, ItemData item)
    {
        GameClient.Instance.Send(Protocol.CS_DropItem, new ClientDropItem
        {
            Slot = slot,
            InstanceId = item.InstanceId
        });
    }
    
    

    #region StorageModel Events
    

    private void OnSlotChanged(SlotKey slot)
    {
        navigationView.UpdateSlotContent(slot);
    }
    
    #endregion

    public bool GetSlotItem(SlotKey slot, out ItemData value)
    {
        return storageModel.TryGetItem(slot, out value);
    }

    public void RequestSwap(SlotKey slotKey, SlotKey targetSlotKey)
    {
        storageModel.RequestSwap(slotKey, targetSlotKey);
    }



}
