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
}