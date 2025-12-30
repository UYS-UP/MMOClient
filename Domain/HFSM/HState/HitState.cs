public class HitState : HState
{
    private readonly EntityFsmContext ctx;
    private const float duration = 1.167f;
    public HitState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
    {
        this.ctx = ctx;
    }

    public bool IsFinished;
    private float currentTime;

    protected override void OnEnter()
    {
        ctx.Animator.CrossFade("Hit", 0.1f);
        ctx.LockMove = true;
        ctx.LockTurn = true;
        ctx.HitRequested = false;
        IsFinished = false;
        currentTime = 0;
    }

    protected override void OnUpdate(float deltaTime)
    {
        currentTime += deltaTime;
        if (currentTime >= duration)
        {
            IsFinished = true;
        }
    }
}