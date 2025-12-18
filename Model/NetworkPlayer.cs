using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;

[MessagePackObject]
public class NetworkPlayer
{
    [Key(0)] public string PlayerId { get; set; }
    [Key(1)] public string Username { get; set; }
    [Key(2)] public string Password { get; set; }
    [Key(3)] public List<NetworkCharacter> Roles { get; set; }
}