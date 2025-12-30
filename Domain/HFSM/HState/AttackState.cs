public class AttackState : HState
{
    private readonly EntityFsmContext ctx;
        
    public AttackState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
    {
        this.ctx = ctx;
    }

    protected override void OnEnter()
    {
        StartSkill(ctx.ComboNextSkillId);
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (ctx.ComboWindowOpen && ctx.ComboRequested)
        {
            int skillId = ctx.ComboNextSkillId;
            StartSkill(skillId);
        }
    }

    private void StartSkill(int skillId)
    {
        ctx.ComboRequested = false; 
        ctx.AttackRequested = false;
        ctx.CastSkill?.Interrupt();
        ctx.CastSkill = new SkillInstance(skillId, ctx.Entity);
        ctx.CastSkill.Start();
    }
}