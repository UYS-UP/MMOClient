
using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInfoController : IDisposable
{
    private readonly StorageModel storageModel;
    private readonly CharacterInfoView characterInfoView;
    
    public CharacterInfoController(CharacterInfoView characterInfoView)
    {
        storageModel = GameContext.Instance.Get<StorageModel>();
        this.characterInfoView = characterInfoView;
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
        characterInfoView.UpdateBatchSlots(slots);
    }

    private void OnSlotChanged(SlotKey slot)
    {
        characterInfoView.UpdateSlotContent(slot);
    }

    private void OnInventoryResized(int newSize)
    {
        characterInfoView.ResizeInventory(newSize);
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
