using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public enum ActivityMode
{
    Inactive,
    Activating,
    Active,
    Deactivating
}

public interface IActivity
{
    ActivityMode Mode { get; }
    UniTask ActivateAsync(CancellationToken ct);
    UniTask DeactivateAsync(CancellationToken ct);
}

public abstract class Activity : IActivity
{
    public ActivityMode Mode { get; protected set; } = ActivityMode.Inactive;
   
    public virtual async UniTask ActivateAsync(CancellationToken ct)
    {
        if(Mode != ActivityMode.Inactive) return;
        Mode = ActivityMode.Activating;
        await UniTask.CompletedTask;
        Mode = ActivityMode.Active;
    }

    public virtual async UniTask DeactivateAsync(CancellationToken ct)
    {
        if(Mode != ActivityMode.Active) return;
        Mode = ActivityMode.Deactivating;
        await UniTask.CompletedTask;
    }
}

public class MoveAnimationActivity : Activity
{
    private readonly EntityFsmContext ctx;
    
    public MoveAnimationActivity(EntityFsmContext ctx)
    {
        this.ctx = ctx;
    }

    public override async UniTask ActivateAsync(CancellationToken ct)
    {
        Debug.Log("开始移动");
        ctx.Animator.CrossFade("MoveStart", 0.1f);
        await UniTask.Delay((int)(0.667 * 1000), cancellationToken: ct);
        Mode = ActivityMode.Active;
        // 【阶段2：循环】
        // 起步播完了，才进入正式移动循环
        ctx.Animator.CrossFade("Move", 0.1f);

        Mode = ActivityMode.Active;
    }

    public override async UniTask DeactivateAsync(CancellationToken ct)
    {
            if (Mode != ActivityMode.Active) return;
            Mode = ActivityMode.Deactivating;
            Debug.Log("停止移动");
            ctx.LockMove = true;
            // 1. 播放急停动画
            ctx.Animator.CrossFade("MoveStop", 0.1f);
    
            // 2. 等待急停动画时长
            // 这里的等待至关重要，它会阻止 TransitionSequencer 立即进入 IdleState
            await UniTask.Delay((int)(0.9 * 1000), cancellationToken: ct);
    
            Mode = ActivityMode.Inactive;
    }
}