using UnityEngine;

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
        if (ctx.DeathRequested) return Parent.AsTo<RootState>()?.Dead;
        if (ctx.HitRequested) return Hit;
        if (ctx.CastRequested)
        {
            if (ActiveChild != Action) return Action;
        }
        
        if (ActiveChild == Action)
        {
            if (ctx.CastSkill == null || ctx.CastSkill.IsFinished)
            {
                return Locomotion;
            }
        }
        
        if (ctx.Entity.IsLocal && ActiveChild == Locomotion)
        {
            if (ctx.AttackRequested || ctx.RollRequested) return Action;
        }

        return null;
    }

    protected override void OnUpdate(float deltaTime)
    {
        ctx.ConsumeOneFrameFlags();
    }
}

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
        ctx.Animator.CrossFade("Death", 0.08f);
    }
    
    protected override HState GetTransition() => null;
}

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
}

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

    protected override HState GetTransition() => ctx.HasMoveInput ? Parent.AsTo<LocomotionState>()?.Move : null;
}

public class MoveState : HState
{
    private readonly EntityFsmContext ctx;
    public MoveState(EntityFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
    {
        this.ctx = ctx;
    }
    
    protected override void OnEnter()
    {
        ctx.Animator.CrossFade("Move", 0.1f);
    }

    protected override void OnUpdate(float deltaTime)
    {
        var worldDir = ctx.WishDir;

        if (ctx.HasMoveInput)
        {
           ctx.Animator.UpdateMovement(worldDir, deltaTime);
        }
    }
    
    protected override HState GetTransition()
        => !ctx.HasMoveInput ? Parent.AsTo<LocomotionState>().Idle : null;
}



public class HitState : HState
{
    private readonly EntityFsmContext ctx;
    public HitState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
    {
        this.ctx = ctx;
    }
}