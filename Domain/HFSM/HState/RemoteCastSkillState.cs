
public class RemoteCastSkillState : HState
{
    private readonly EntityFsmContext ctx;
    
    public RemoteCastSkillState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
    {
        this.ctx = ctx;
    }
    
    protected override void OnEnter()
    {
        CastCurrentSkill();
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (ctx.CastRequested)
        {
            CastCurrentSkill();
        }
    }

    private void CastCurrentSkill()
    {
        ctx.CastRequested = false;
        
        ctx.CastSkill?.Interrupt();
        ctx.CastSkill = new SkillInstance(ctx.CastSkillId, ctx.Entity);
        ctx.CastSkill.Start();
    }
}
