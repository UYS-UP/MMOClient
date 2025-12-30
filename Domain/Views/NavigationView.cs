using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NavigationView : BaseView
{
    private NavigationController controller;
    public ProfileView ProfileView;
    public InventoryView InventoryView;
    
    protected override void Awake()
    {
        base.Awake();
        
        controller = new NavigationController(this);
        InventoryView.Initialize(controller);
        ProfileView.Initialize(controller);
    }

    public void OpenProfile(string characterName, string characterGuildName, string characterTitle, int currentLevel, int maxLevel, float currentEx, float maxEx)
    {
        ProfileView.gameObject.SetActive(true);
        ProfileView.OpenProfile(
            characterName, 
            characterGuildName, 
            characterTitle, 
            currentLevel, 
            maxLevel, 
            currentEx, 
            maxEx);
    }

    public void OpenInventory(int maxSize)
    {
        ProfileView.gameObject.SetActive(false);
        InventoryView.gameObject.SetActive(true);

        InventoryView.OpenInventory(maxSize);
    }
    
    private void OnSortButtonClick()
    {
        // 排序
    }
    
    private void OnDestroy()
    {
        controller?.Dispose();
    }

    public void UpdateSlotsContent(IReadOnlyList<SlotKey> slots)
    {
        InventoryView.UpdateSlotsContent(slots);
    }

    public void UpdateSlotContent(SlotKey slot)
    {
        InventoryView.UpdateSlotContent(slot);
    }

    public void ResizeInventory(int newSize)
    {
        InventoryView.ResizeInventory(newSize);
    }
    
    
    
    public void RequestSwap(SlotKey slotKey, SlotKey targetSlotKey)
    {
        controller.RequestSwap(slotKey, targetSlotKey);
    }
}