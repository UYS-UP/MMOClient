public class RootState : HState
{
    private readonly EntityFsmContext ctx;
    public AliveState Alive;
    public DeadState Dead;
    
    public RootState(EntityFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
    {
        this.ctx = ctx;
        Alive = new AliveState(ctx, machine, this);
        Dead = new DeadState(ctx, machine, this);
    }

    protected override HState GetInitialState() => Alive;

    protected override HState GetTransition()
    {
        if (ctx.DeathRequested) return Dead;
        return null;
    }
}