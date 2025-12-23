
using UnityEngine;

public class OpenComboWindowPhase : SkillPhase
{
    public float Duration;
    public int Next;
    
    public override void OnStart(EntityBase caster)
    {
        if(!caster.IsLocal) return;
        var ctx = caster.FSM.Ctx;
        ctx.ComboWindowOpen = true;
        ctx.ComboNextSkillId = Next;
    }

    public override void OnExit(EntityBase caster)
    {
        if(!caster.IsLocal) return;
        var ctx = caster.FSM.Ctx;
        ctx.ComboWindowOpen = false;
        ctx.ComboNextSkillId = 0;
        ctx.ComboRequested = false; 
    }
}
