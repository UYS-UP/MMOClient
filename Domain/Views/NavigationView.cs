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
        controller.ApplyFilter(string.Empty, QualityType.None, ItemType.None);
    }
    
    public void ResetScrollPosition()
    {
        InventoryView.ResetScrollPosition();
    }
    
    private void OnDestroy()
    {
        controller?.Dispose();
    }

    public void UpdateSlotContent(SlotKey slot)
    {
        InventoryView.UpdateSlotContent(slot);
    }
    
    
    public void RequestSwap(SlotKey slotKey, SlotKey targetSlotKey)
    {
        controller.RequestSwap(slotKey, targetSlotKey);
    }

    public void UpdateDisplayList(List<SlotKey> displayList)
    {
        InventoryView.UpdateDisplayList(displayList);
    }
}