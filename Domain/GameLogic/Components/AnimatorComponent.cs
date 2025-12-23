using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 动画控制器，管理Animator参数和动画过渡
/// </summary>
public class AnimatorComponent : BaseComponent
{
    
    public readonly Animator animator;
    private EntityBase entity;
    
    private readonly int horizontalHash = Animator.StringToHash("Horizontal");
    private readonly int verticalHash = Animator.StringToHash("Vertical");

    private const float DAMP = 0.05f; // 平滑时间
    private Vector3 lastPos;
    private bool hasLast;
    private Vector3 smoothedVel;
    private const float VEL_SMOOTH_K = 10f; // 速度低通
    private const float MAX_ANIM_SPEED = 6.0f; // 动画速度上限，略大于跑步速度
    
    public AnimatorComponent(Animator animator)
    {
        this.animator = animator;
    }
    
    public override void Attach(EntityBase entity)
    {
        this.entity = entity;
    }
    

    public void CrossFade(string state, float duration)
    {
        animator.CrossFade(state, duration);
    }

    public void CrossFadeInFixedTime(string state, float duration)
    {
        animator.CrossFadeInFixedTime(state, duration);
    }

    public void Play(string state)
    {
        animator.Play(state);
    }


    public void UpdateMovement(Vector3 worldDir, float deltaTime)
    {
        var localDir = entity.transform.InverseTransformDirection(worldDir);
        animator.SetFloat(horizontalHash, localDir.x, 0.1f, deltaTime);
        animator.SetFloat(verticalHash, localDir.z, 0.1f, deltaTime);
    }
    


    public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layer)
    {
        return animator.GetCurrentAnimatorStateInfo(layer);
    }

    public bool IsInTransition(int layer)
    {
        return animator.IsInTransition(layer);
    }
    
    public override void OnAnimatorMove()
    {
        // Vector3 delta = animator.deltaPosition;
        //
        // delta.y = 0f;
        //
        // Vector3 fwd = entity.transform.forward;
        // fwd.y = 0f;
        // fwd.Normalize();
        // delta = Vector3.Project(delta, fwd);
        //
        // var move = entity.GetEntityComponent<LocalMoveComponent>();
        // move?.AddExternalMotion(delta);
    }
    
    
}