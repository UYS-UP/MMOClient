
using UnityEngine;

public class ActionState : HState
{
    private readonly EntityFsmContext ctx;
    public readonly AttackState Attack;
    public readonly RollState Roll;
    public readonly CastSkillState CastSkill;
    public readonly RemoteCastSkillState RemoteCastSkill;
    
    
    public ActionState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
    {
        this.ctx = ctx;
        Attack = new AttackState(ctx, m, this);
        Roll = new RollState(ctx, m, this);
        CastSkill = new CastSkillState(ctx, m, this);
        RemoteCastSkill = new RemoteCastSkillState(ctx, m, this);
    }
    
    
    protected override HState GetInitialState()
    {

        if (ctx.CastRequested)
        {
            if(ctx.Entity.IsLocal) return CastSkill;
            return RemoteCastSkill;
        }
        if (ctx.RollRequested) return Roll;
        if (ctx.AttackRequested) return Attack;
        return Attack; 
    }

    protected override void OnEnter()
    {
        ctx.LockMove = true;
        ctx.LockTurn = true;
    }

    protected override void OnUpdate(float deltaTime)
    {
        ctx.CastSkill?.Update(deltaTime);
    }
    
    

    protected override void OnExit()
    {
        if (ctx.CastSkill != null)
        {
            ctx.CastSkill.Interrupt();
            ctx.CastSkill = null;
        }
        ctx.LockMove = false;
        ctx.LockTurn = false;
    }

    protected override HState GetTransition()
    {
        if (ctx.CastRequested && ActiveChild != CastSkill && ActiveChild != RemoteCastSkill)
        {
            if(ctx.Entity.IsLocal) return CastSkill;
            return RemoteCastSkill;
        }
        if (ActiveChild != Roll && ctx.RollRequested) return Roll;
        if (ActiveChild == Attack && ctx.CastRequested) return CastSkill;
        return null;
    }
}


public class AttackState : HState
{
    private readonly EntityFsmContext ctx;
        
    public AttackState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
    {
        this.ctx = ctx;
    }

    protected override void OnEnter()
    {
        CastCurrentSkill(ctx.ComboNextSkillId);
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (ctx.ComboWindowOpen && ctx.ComboRequested)
        {
            int skillId = ctx.ComboNextSkillId;
            CastCurrentSkill(skillId);
        }
    }

    private void CastCurrentSkill(int skillId)
    {
        ctx.ComboRequested = false; 
        ctx.AttackRequested = false;
        ctx.CastSkill?.Interrupt();
        ctx.CastSkill = new SkillInstance(skillId, ctx.Entity);
        ctx.CastSkill.Start();
    }
}

public class RollState : HState
{
    private readonly EntityFsmContext ctx;
        
    public RollState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
    {
        this.ctx = ctx;
    }
    
    protected override void OnEnter()
    {
        ctx.RollRequested = false;

        var rollId = CalculateDirectionalRoll();
        ctx.CastSkill = new SkillInstance(rollId, ctx.Entity);
        ctx.CastSkill.Start();
    }

    private int CalculateDirectionalRoll()
    {
        if (!ctx.HasMoveInput || ctx.WishDir.sqrMagnitude <= 0.001f)
        {
            return 100;
        }

        var localDir = ctx.Entity.transform.InverseTransformDirection(ctx.WishDir);
        float x = localDir.x;
        float z = localDir.z;
        float absX = Mathf.Abs(x);
        float absZ = Mathf.Abs(z);
        
        if (absZ >= absX)
        {
            // 纵向为主 (前后)
            return z > 0 ? 100 : 101;
        }
        else
        {
            // 横向为主 (左右)
            return x > 0 ? 103 : 102;
        }
    }
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
        
        // 消费输入
        int skillId = ctx.CastSkillId;
        ctx.CastRequested = false;
        
        ctx.CastSkill?.Interrupt();
        ctx.CastSkill = new SkillInstance(skillId, ctx.Entity);
        ctx.CastSkill.Start();
    }
    
    
}
