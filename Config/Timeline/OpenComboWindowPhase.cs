
using UnityEngine;

public class OpenComboWindowPhase : SkillPhase
{
    public float Duration;
    public int Next;
    
    public override void OnStart(EntityBase caster)
    {
        Debug.Log("开启连击窗口,下一段攻击");
        var ctx = caster.FSM.Ctx;
        ctx.ComboWindowOpen = true;
        ctx.ComboNextSkillId = Next;
    }

    public override void OnExit(EntityBase caster)
    {
        Debug.Log("关闭连击窗口");
        var ctx = caster.FSM.Ctx;
        ctx.ComboWindowOpen = false;
        ctx.ComboNextSkillId = -1;
        ctx.ComboRequested = false; 
    }
}
