public class IdleState : HState
{
    private readonly EntityFsmContext ctx;
    public IdleState(EntityFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
    {
        this.ctx = ctx;
    }

    protected override void OnEnter()
    {
        ctx.Animator.CrossFade("Idle", 0.1f);
    }
}