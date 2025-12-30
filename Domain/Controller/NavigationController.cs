
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
        storageModel.OnSlotsChanged += OnSlotsChanged;
        storageModel.OnSlotChanged += OnSlotChanged;
        storageModel.OnInventoryResized += OnInventoryResized;
    }

    private void UnregisterEvents()
    {
        storageModel.OnSlotsChanged -= OnSlotsChanged;
        storageModel.OnSlotChanged -= OnSlotChanged;
        storageModel.OnInventoryResized -= OnInventoryResized;
    }
    

    #region StorageModel Events

    private void OnSlotsChanged(IReadOnlyList<SlotKey> slots)
    {
        navigationView.UpdateSlotsContent(slots);
    }

    private void OnSlotChanged(SlotKey slot)
    {
        navigationView.UpdateSlotContent(slot);
    }

    private void OnInventoryResized(int newSize)
    {
        navigationView.ResizeInventory(newSize);
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
