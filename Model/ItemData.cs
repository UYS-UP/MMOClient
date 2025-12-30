    using System;
    using MessagePack;
    using UnityEngine;

    
    public enum ItemType : byte
    {
        Equip = 0,
        Consumable = 1,
        Material  = 2
    }

    public enum QuantityType : byte
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3
    }

    [MessagePackObject]
    [Union(0, typeof(EquipData))]
    [Union(1, typeof(ConsumableData))]
    public abstract class ItemData
    {
        [Key(0)] public string ItemId { get; set; }
        [Key(1)] public string ItemTemplateId { get; set; }
        [Key(2)] public string ItemName { get; set; }
        [Key(3)] public ItemType ItemType { get; set; }
        [Key(4)] public QuantityType QuantityType { get; set; }
        [Key(5)] public string Description { get; set; }
        [Key(6)] public int Gold { get; set; }
        [Key(7)] public bool IsStack {  get; set; }
        [Key(8)] public int ItemCount { get; set; }

    }

    public enum EquipType : byte
    {
        Weapon = 0,
        Helmet = 1,
        Clothes = 2,
        Pants = 3,
        Shoes = 4,
        Necklace = 5,
        Earring = 6
    }

    [MessagePackObject]
    public class EquipData : ItemData
    {
        [Key(9)] public int Health { get; set; }
        [Key(10)] public int Mana { get; set; }
        [Key(11)] public int AttackPower { get; set; }
        [Key(12)] public int DefencePower { get; set; }
        [Key(13)] public int SpellPower { get; set; }
        [Key(14)] public int MagicResistance { get; set; }
        [Key(15)] public int CriticalHitRate { get; set; }
        [Key(16)] public EquipType EquipType { get; set; }
        [Key(17)] public int Level { get; set; }
    }


    public enum EffectType : byte
    {
        HealHP = 0,
        HealMP = 1,
    }

    [MessagePackObject]
    public class ConsumableData : ItemData
    {
        [Key(9)] public EffectType EffectType { get; set; }
        [Key(10)] public int EffectValue { get; set; }
        [Key(11)] public float Cooldown { get; set; }
    }