public class AliveState : HState
{
    private readonly EntityFsmContext ctx;
    public readonly LocomotionState Locomotion;
    public readonly ActionState Action;
    public readonly HitState Hit;

    public AliveState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
    {
        this.ctx = ctx;
        Locomotion = new LocomotionState(ctx, m, this);
        Hit = new HitState(ctx, m, this);
        Action = new ActionState(ctx, m, this);
    }

    protected override HState GetInitialState() => Locomotion;

    protected override HState GetTransition()
    {
        if (ActiveChild != Hit && ctx.HitRequested) return Hit;
        
        if (ActiveChild == Locomotion)
        {
            if (ctx.CastRequested) return Action;
            
            if (ctx.Entity.IsLocal)
            {
                if (ctx.AttackRequested || ctx.RollRequested) return Action;
            }
        }else if (ActiveChild == Action)
        {
            if (Action.IsFinished)
                return Locomotion;
        }else if (ActiveChild == Hit)
        {
            if (Hit.IsFinished) return Locomotion;
        }
        
        return null;
    }
    
}