
using UnityEngine;

public class ActionState : HState
{
    private readonly EntityFsmContext ctx;
    public readonly AttackState Attack;
    public readonly RollState Roll;
    public readonly CastSkillState CastSkill;
    public readonly RemoteCastSkillState RemoteCastSkill;
    
    public bool IsFinished => ctx.CastSkill?.IsFinished ?? false;
    
    public ActionState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
    {
        this.ctx = ctx;
        Attack = new AttackState(ctx, m, this);
        Roll = new RollState(ctx, m, this);
        CastSkill = new CastSkillState(ctx, m, this);
        RemoteCastSkill = new RemoteCastSkillState(ctx, m, this);
    }
    
    
    protected override HState GetInitialState()
    {

        if (ctx.CastRequested)  return ctx.Entity.IsLocal ? CastSkill : RemoteCastSkill;
        if (ctx.RollRequested) return Roll;
        if (ctx.AttackRequested) return Attack;
        return Attack; 
    }

    protected override void OnEnter()
    {
        ctx.LockMove = true;
        ctx.LockTurn = true;

    }

    protected override void OnUpdate(float deltaTime)
    {
        ctx.CastSkill?.Update(deltaTime);
    }
    
    

    protected override void OnExit()
    {
        if (ctx.CastSkill != null)
        {
            ctx.CastSkill.Interrupt();
            ctx.CastSkill = null;
        }
        ctx.LockMove = false;
        ctx.LockTurn = false;
    }

    protected override HState GetTransition()
    {
        if (ActiveChild != Roll && ctx.RollRequested) return Roll;
        
        if (ActiveChild == Attack && ctx.CastRequested)
            return ctx.Entity.IsLocal ? CastSkill : RemoteCastSkill;

        return null;
    }
}







