using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerAction
{
    MoveForward,
    MoveBackward,
    MoveLeft,
    MoveRight,
    
    Attack,
    Skill1,
    Skill2,
    Skill3,
    
    Interact,
    MouseRight,
    
    OpenInventory,
    Dialogue
}

[Serializable]
public class ActionBinding
{
    public PlayerAction Action;
    public KeyCode Primary = KeyCode.None;
    public KeyCode Alternative = KeyCode.None;
}

public class InputBindService : SingletonMono<InputBindService>
{
    [SerializeField]
    private List<ActionBinding> defaultBindings = new List<ActionBinding>();
    
    private readonly Dictionary<PlayerAction, (KeyCode primary, KeyCode alternative)> map = new Dictionary<PlayerAction, (KeyCode, KeyCode)>();

    private const string PREF_KEY = "keymap_";

    public bool UIIsOpen { get; set; } = false;
    public bool AttackBan { get; set; } = false;

    protected override void Awake()
    {
        base.Awake();
        Load();
        map[PlayerAction.MouseRight] = (KeyCode.Mouse1, KeyCode.Mouse1);
        map[PlayerAction.MoveForward] = (KeyCode.W, KeyCode.W);
        map[PlayerAction.MoveBackward] = (KeyCode.S, KeyCode.S);
        map[PlayerAction.MoveLeft] = (KeyCode.A, KeyCode.A);
        map[PlayerAction.MoveRight] = (KeyCode.D, KeyCode.D);
        map[PlayerAction.Attack] = (KeyCode.Mouse0,  KeyCode.Mouse0);
        map[PlayerAction.OpenInventory] =  (KeyCode.B,  KeyCode.B);
        map[PlayerAction.Dialogue] = (KeyCode.E, KeyCode.E);
        map[PlayerAction.Skill1] = (KeyCode.Alpha1, KeyCode.Alpha1);
    }
    
    public KeyCode GetPrimary(PlayerAction a) => map.TryGetValue(a, out var k) ? k.primary : KeyCode.None;
    public KeyCode GetAlt(PlayerAction a) => map.TryGetValue(a, out var k) ? k.alternative : KeyCode.None;
    
    public void SetPrimary(PlayerAction a, KeyCode key)
    {
        map[a] = (key, GetAlt(a)); 
        SaveOne(a);
    }

    public void SetAlt(PlayerAction a, KeyCode key)
    {
        map[a] = (GetPrimary(a), key); 
        SaveOne(a);
    }
    
    
    public bool IsPressed(PlayerAction a)
    {
        var p = GetPrimary(a); var alt = GetAlt(a);
        return (p != KeyCode.None && Input.GetKey(p)) || (alt != KeyCode.None && Input.GetKey(alt));
    }
    
    public bool IsDown(PlayerAction a)
    {
        var p = GetPrimary(a); var alt = GetAlt(a);
        return (p != KeyCode.None && Input.GetKeyDown(p)) || (alt != KeyCode.None && Input.GetKeyDown(alt));
    }

    public bool IsUp(PlayerAction a)
    {
        var p = GetPrimary(a); 
        var alt = GetAlt(a);
        return (p != KeyCode.None && Input.GetKeyUp(p)) || (alt != KeyCode.None && Input.GetKeyUp(alt));;
    }
    
    private void Load()
    {
        map.Clear();
        foreach (var b in defaultBindings)
        {
            var p = (KeyCode)PlayerPrefs.GetInt(PREF_KEY + b.Action + "_p", (int)b.Primary);
            var a = (KeyCode)PlayerPrefs.GetInt(PREF_KEY + b.Action + "_a", (int)b.Alternative);
            map[b.Action] = (p, a);
        }
    }
    
    private void SaveOne(PlayerAction action)
    {
        var (p, a) = map[action];
        PlayerPrefs.SetInt(PREF_KEY + action + "_p", (int)p);
        PlayerPrefs.SetInt(PREF_KEY + action + "_a", (int)a);
        PlayerPrefs.Save();
    }
}
