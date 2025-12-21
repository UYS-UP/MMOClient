using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;


[Serializable]
public abstract class SkillEvent
{
    public float Time { get; set; }
    public virtual void Execute(EntityBase caster){}
    
}

[Serializable]
public abstract class SkillPhase
{
    public float StartTime  { get; set; }
    public float EndTime { get; set; }
    public virtual void OnStart(EntityBase caster){}
    public virtual void OnExit(EntityBase caster){}
    public virtual void OnUpdate(EntityBase caster, float dt){}
   
}

[Serializable]
public class SkillTimelineConfig
{
    public int Id { get; set; }
    public string Name { get; set; }
    public float Duration { get; set; }
    
    public List<SkillEvent> ClientEvents {get; set;} = new List<SkillEvent>();
    public List<SkillEvent> ServerEvents {get; set;} = new List<SkillEvent>();
    public List<SkillPhase> ClientPhases {get; set;} = new List<SkillPhase>();
    public List<SkillPhase> ServerPhases {get; set;} = new List<SkillPhase>();
}
