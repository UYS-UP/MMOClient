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
    public LocomotionState Locomotion;
    public CastSkillState CastSkill;
    public HitState Hit;

    public AliveState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
    {
        this.ctx = ctx;
        Locomotion = new LocomotionState(ctx, m, this);
        CastSkill = new CastSkillState(ctx, m, this);
        Hit = new HitState(ctx, m, this);
    }

    protected override void OnEnter()
    {
        Debug.Log("Enter: " + HStateMachine.StatePath(this));
    }

    protected override HState GetInitialState() => Locomotion;

    protected override HState GetTransition()
    {
        if (ctx.DeathRequested) return Parent.AsTo<RootState>()?.Dead;
        if (ctx.HitRequested) return Hit;
        if (ctx.CastRequested)
        {
            if (CastSkill == null || ctx.CastSkill == null || ctx.CastSkill.IsFinished)
            {
                return CastSkill;
            }
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
        Debug.Log("Enter: " + HStateMachine.StatePath(this));
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
        Debug.Log("Enter: " + HStateMachine.StatePath(this));
        this.ctx = ctx;
        Idle = new IdleState(ctx, m, this);
        Move = new MoveState(ctx, m, this);
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
        Debug.Log("Enter: " + HStateMachine.StatePath(this));
        ctx.LockMove = false;
        ctx.LockTurn = false;
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
        Add(new MoveAnimationActivity(ctx));
    }
    
    protected override void OnEnter()
    {
        Debug.Log("Enter: " + HStateMachine.StatePath(this));
        ctx.LockMove = false;
        ctx.LockTurn = false;
        // ctx.Animator.CrossFadeInFixedTime("Move", 0.1f);
 
    }
    
    protected override HState GetTransition()
        => !ctx.HasMoveInput ? Parent.AsTo<LocomotionState>().Idle : null;
}

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
    

        int skillId = ctx.CastSkillId;
        ctx.CastRequested = false;
        ctx.CastSkill = new SkillInstance(skillId, ctx.Entity);
        ctx.CastSkill.Start();
    }

    protected override void OnUpdate(float deltaTime)
    {
        ctx.CastSkill?.Update(deltaTime);


        if (ctx.Entity.IsLocal)
        {
            bool wantCombo = ctx.ComboRequested;
            ctx.ComboRequested = false; // 严格窗口：不缓冲

            if (ctx.ComboWindowOpen && wantCombo && ctx.ComboNextSkillId >= 0)
            {
                int nextId = ctx.ComboNextSkillId;

                ctx.CastSkill?.Interrupt();
                ctx.CastSkill = new SkillInstance(nextId, ctx.Entity);
            
                ctx.CastSkill.Start();

            }
        }
        else
        {
            if (ctx.CastRequested)
            {

                int nextId = ctx.CastSkillId;
                ctx.CastRequested = false;
                ctx.CastSkill?.Interrupt();
                ctx.CastSkill = new SkillInstance(nextId, ctx.Entity);
            
                ctx.CastSkill.Start();
            }
        }
        

    }

    protected override void OnExit()
    {
        ctx.CastSkill?.Interrupt();
        ctx.CastSkill = null;
    }

    protected override HState GetTransition()
    {
        if (ctx.DeathRequested) return Parent.AsTo<AliveState>()?.Parent.AsTo<RootState>()?.Dead;
        if (ctx.HitRequested)   return Parent.AsTo<AliveState>()?.Hit;
        
        if (ctx.CastSkill == null || ctx.CastSkill.IsFinished)
            return Parent.AsTo<AliveState>()?.Locomotion;

        return null;
    }
    
}

public class HitState : HState
{
    private readonly EntityFsmContext ctx;
    public HitState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
    {
        this.ctx = ctx;
    }
}