using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HfsmCharacterControllerDemo : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float airControl = 0.6f;

    [Header("Jump / Gravity")]
    public float jumpHeight = 1.4f;
    public float gravity = -18f;              // 负数
    public float groundedStick = -2f;         // 贴地小下压，避免离地抖动

    [Header("Debug")]
    public bool logStateChanges = true;

    private CharacterController cc;
    private DemoContext ctx;
    private HStateMachine sm;

    // ---- States (kept as references, no reflection hacks) ----
    private RootState root;
    private GroundState ground;
    private IdleState idle;
    private MoveState move;
    private JumpState jump;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        var renderer = GetComponentInChildren<Renderer>();
        ctx = new DemoContext
        {
            Controller = cc,
            Transform = transform,
            Gravity = gravity,
            JumpHeight = jumpHeight,
            GroundedStick = groundedStick,
            MoveSpeed = moveSpeed,
            AirControl = airControl,
            Log = logStateChanges,
            Renderer = renderer,
            OriginalMaterial = renderer.material,
            JumpMaterial = new Material(renderer.material)
            {
                color = Color.red   // 跳跃时变红（你可以换成任意材质）
            },
        };

        // Build HFSM
        sm = new HStateMachine();
        root = new RootState(ctx, sm, null);



        ground = new GroundState(ctx, sm, root);
        idle = new IdleState(ctx, sm, ground);
        move = new MoveState(ctx, sm, ground);
        jump = new JumpState(ctx, sm, root);

        // Wire references
        root.Ground = ground;
        root.Jump = jump;

        ground.Idle = idle;
        ground.Move = move;
        sm.SetRoot(root);
        sm.Start();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1) Input -> Intent
        ctx.MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        ctx.JumpPressed = Input.GetKeyDown(KeyCode.Space);

        // 2) Sensors (grounded) comes from CharacterController after previous Move,
        // but we also do a lightweight update here.
        ctx.IsGrounded = cc.isGrounded;
        
        // 3) HFSM tick (decide desired state)
        sm.Update(dt);

        // 4) Motor (execute movement) based on current state + intent
        MotorTick(dt);

        // 5) Clear one-frame input
        ctx.JumpPressed = false;
    }

    private void MotorTick(float dt)
    {
        // Horizontal desired velocity (local input -> world direction)
        Vector3 inputWorld = (transform.right * ctx.MoveInput.x + transform.forward * ctx.MoveInput.y);
        if (inputWorld.sqrMagnitude > 1f) inputWorld.Normalize();

        float control = ctx.InAir ? ctx.AirControl : 1f;
        Vector3 horizontal = inputWorld * (ctx.MoveSpeed * control);

        // Gravity integration
        if (ctx.IsGrounded)
        {
            // stick to ground
            if (ctx.VerticalVelocity < 0f)
                ctx.VerticalVelocity = ctx.GroundedStick;

            // Jump impulse (only when HFSM state is Jump-entering, we set a flag)
            if (ctx.ConsumeJumpImpulse)
            {
                ctx.VerticalVelocity = Mathf.Sqrt(2f * ctx.JumpHeight * -ctx.Gravity);
                ctx.ConsumeJumpImpulse = false;
                ctx.IsGrounded = false;
            }
        }
        else
        {
            ctx.VerticalVelocity += ctx.Gravity * dt;
        }

        Vector3 velocity = horizontal + Vector3.up * ctx.VerticalVelocity;

        // Move
        cc.Move(velocity * dt);

        // Update grounded after move (more accurate)
        ctx.IsGrounded = cc.isGrounded;

        // If we landed, clamp vertical vel so we don't accumulate negative speed
        if (ctx.IsGrounded && ctx.VerticalVelocity < 0f)
            ctx.VerticalVelocity = ctx.GroundedStick;
    }

    private sealed class DemoContext
    {
        public CharacterController Controller;
        public Transform Transform;

        // ===== 新增：渲染相关 =====
        public Renderer Renderer;
        public Material OriginalMaterial;
        public Material JumpMaterial;

        public Vector2 MoveInput;
        public bool JumpPressed;

        public bool IsGrounded;
        public float VerticalVelocity;

        public float MoveSpeed;
        public float AirControl;

        public float Gravity;
        public float JumpHeight;
        public float GroundedStick;

        public bool ConsumeJumpImpulse;
        public bool InAir;

        public bool Log;
        public void LogState(string msg)
        {
            if (Log) Debug.Log(msg);
        }
    }


    private sealed class RootState : HState
    {
        private readonly DemoContext Ctx;
        public GroundState Ground;
        public JumpState Jump;

        public RootState(DemoContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            Ctx = ctx;
        }

        protected override HState GetInitialState() => Ground;
        protected override HState GetTransition() => Ctx.IsGrounded  ? null : Jump;

        protected override void OnEnter() => Ctx.LogState("[Enter] Root");
        protected override void OnExit() => Ctx.LogState("[Exit ] Root");
    }

    private sealed class GroundState : HState
    {
        private readonly DemoContext Ctx;
        public IdleState Idle;
        public MoveState Move;

        public GroundState(DemoContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            Ctx = ctx;
        }

        protected override HState GetInitialState()
        {
            
            return Ctx.MoveInput.sqrMagnitude > 0.01f ? (HState)Move : Idle;
        }

        protected override HState GetTransition()
        {
            if (!Ctx.IsGrounded) return ((RootState)Parent).Jump;

            if (Ctx.JumpPressed && Ctx.IsGrounded)
                return ((RootState)Parent).Jump;

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.InAir = false;
            Ctx.LogState("[Enter] Ground");
        }
        
    }

    private sealed class IdleState : HState
    {
        private readonly DemoContext Ctx;

        public IdleState(DemoContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            Ctx = ctx;
        }

        protected override HState GetTransition()
        {
            if (Ctx.MoveInput.sqrMagnitude > 0.01f) return ((GroundState)Parent).Move;
            return null;
        }

        protected override void OnEnter() => Ctx.LogState("[Enter] Idle");
        protected override void OnExit() => Ctx.LogState("[Exit ] Idle");
    }

    private sealed class MoveState : HState
    {
        private readonly DemoContext Ctx;

        public MoveState(DemoContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            Ctx = ctx;
        }

        protected override HState GetTransition()
        {
            if (Ctx.MoveInput.sqrMagnitude <= 0.01f) return ((GroundState)Parent).Idle;
            return null;
        }

        protected override void OnEnter() => Ctx.LogState("[Enter] Move");
        protected override void OnExit() => Ctx.LogState("[Exit ] Move");
    }

    private sealed class JumpState : HState
    {
        private readonly DemoContext Ctx;

        public JumpState(DemoContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            var changeMat = new ChangeMaterialActivity(ctx.Renderer, ctx.JumpMaterial);
            Add(changeMat);
            Ctx = ctx;
        }

        protected override HState GetTransition()
        {
            // 落地回 Ground（Ground 会自动选 Idle/Move）
            if (Ctx.IsGrounded && Ctx.VerticalVelocity <= 0f)
                return ((RootState)Parent).Ground;

            return null;
        }

        protected override void OnEnter()
        {
            Ctx.InAir = true;
            Ctx.LogState("[Enter] Jump");

            // 只在地面且按下跳跃时给予一次冲量
            if (Ctx.IsGrounded || Ctx.VerticalVelocity <= 0f)
                Ctx.ConsumeJumpImpulse = true;
        }

        protected override void OnExit() => Ctx.LogState("[Exit ] Jump");
    }
    
    
    public sealed class ChangeMaterialActivity : IActivity
    {
        private readonly Renderer renderer;
        private readonly Material target;
        private Material original;

        public ActivityMode Mode { get; private set; } = ActivityMode.Inactive;

        public ChangeMaterialActivity(Renderer renderer, Material target)
        {
            this.renderer = renderer;
            this.target = target;
        }

        public async UniTask ActivateAsync(CancellationToken ct)
        {
            if (renderer == null) return;
            Debug.Log("Activate");
            original = renderer.material;
            renderer.material = target;
            Mode = ActivityMode.Active;

            await UniTask.CompletedTask;
        }

        public async UniTask DeactivateAsync(CancellationToken ct)
        {
            if (renderer == null) return;

            renderer.material = original;
            Mode = ActivityMode.Inactive;

            await UniTask.CompletedTask;
        }
    }

}
