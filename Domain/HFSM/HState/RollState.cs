using UnityEngine;

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