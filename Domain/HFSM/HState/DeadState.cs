public class DeadState : HState
{
    private readonly EntityFsmContext ctx;

    public DeadState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
    {

        this.ctx = ctx;
    }

    protected override void OnEnter()
    {
        ctx.LockMove = true;
        ctx.LockTurn = true;
        ctx.Animator.CrossFade("Death", 0.1f);
    }
}