public class LocomotionState : HState
{
    private readonly EntityFsmContext ctx;
    public IdleState Idle;
    public MoveState Move;

    public LocomotionState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
    {

        this.ctx = ctx;
        Idle = new IdleState(ctx, m, this);
        Move = new MoveState(ctx, m, this);
    }

    protected override void OnEnter()
    {
        ctx.LockMove = false;
        ctx.LockTurn = false;
    }

    protected override HState GetInitialState() => ctx.HasMoveInput ? Move : Idle;
    
    protected override HState GetTransition()
    {
        if (ActiveChild == Idle && ctx.HasMoveInput) return Move;
        if (ActiveChild == Move && !ctx.HasMoveInput) return Idle;
        return null;
    }
}