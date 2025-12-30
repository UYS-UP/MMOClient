public class CastSkillState : HState
{
    private readonly EntityFsmContext ctx;
        
    public CastSkillState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
    {
        this.ctx = ctx;
    }

    protected override void OnEnter()
    {

        ctx.LockMove = true;
        ctx.LockTurn = false;
        
        // 消费输入
        int skillId = ctx.CastSkillId;
        ctx.CastRequested = false;
        
        ctx.CastSkill?.Interrupt();
        ctx.CastSkill = new SkillInstance(skillId, ctx.Entity);
        ctx.CastSkill.Start();
    }
    
    
}