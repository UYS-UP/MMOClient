    using System;
    using System.Collections.Generic;
    using MessagePack;
    using UnityEngine;

    public enum ItemType
    {
        None,
        Equip,
        Consumable,
        Material
    }

    public enum QualityType
    {
        None,
        Common,
        Uncommon,
        Rare,
        Epic
    }
    
    public enum AttributeType
    {
        None,
        MaxHp,
        MaxEx,
        MaxStamina,
        Attack,
        Defence,
        CritRate,
        CritDamage,
        MovementSpeed,
        CooldownReduction
    }

    [MessagePackObject]
    [Union(0, typeof(EquipData))]
    [Union(1, typeof(ConsumableData))]
    public abstract class ItemData
    {
        [Key(1)] public string TemplateId { get; set; }
        [Key(2)] public string InstanceId { get; set; }
        [Key(3)] public string ItemName { get; set; }
        [Key(4)] public ItemType ItemType { get; set; }
        [Key(5)] public QualityType QuantityType { get; set; }
        [Key(6)] public string Description { get; set; }
        [Key(7)] public int Price { get; set; }
        [Key(8)] public bool IsStack { get; set; }
        [Key(9)] public int ItemCount { get; set; }

    }

    public enum EquipType
    {
        None,
        Weapon,
        Helmet,
        Clothes,
        Pants,
        Shoes,
        Necklace
    }

    [MessagePackObject]
    public class EquipData : ItemData
    {
        [Key(20)] public EquipType EquipType { get; set; }
        [Key(21)] public WeaponType WeaponSubType { get; set; }
        [Key(22)] public int ForgeLevel { get; set; }
        [Key(23)] public Dictionary<AttributeType, float> BaseAttributes { get; set; }
    }

    public enum WeaponType
    {
        None,
        Katana,   
        Bow,       
        GreatSword 
    }


    [MessagePackObject]
    public class WeaponMasteryData
    {
        [Key(0)] public WeaponType WeaponType { get; set; }
        [Key(1)] public int MasteryLevel { get; set; }
        [Key(2)] public int MasteryExp { get; set; }
        [Key(3)] public int SkillPoints { get; set; }
        [Key(4)] public List<int> UnlockedNodes { get; set; }
        [Key(5)] public int[] EquippedSkillIds { get; set; }
    }


    public enum EffectType : byte
    {
        HealHP = 0,
        HealMP = 1,
    }

    [MessagePackObject]
    public class ConsumableData : ItemData
    {
        [Key(20)] public EffectType EffectType { get; set; }
        [Key(21)] public float EffectValue { get; set; }
        [Key(22)] public float Cooldown { get; set; }
    }