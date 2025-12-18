using System.Collections.Generic;
using UnityEngine;

public class LocalMoveComponent : BaseComponent
{
    private EntityBase entity;
    private InputComponent input;
    private CameraComponent cameraComponent;
    private CharacterController characterController;
    private LocalSkillComponent localSkill;
    private Vector3 externalMotion;
    private Vector3 velocity;
    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;

    private const float GRAVITY = -15f;
    private const float TERMINAL_VELOCITY = -20f;

    private int lastSentTick;
    private readonly Queue<Snapshot> snapshots = new Queue<Snapshot>();
    private const int SEND_EVERY_TICKS = 3;
    
    private MotionStateType lastSentMotionState;
    private float lastSentYaw;
    private Vector3 lastSentDir;

    public float TargetYaw;

    public override void Attach(EntityBase e)
    {
        entity = e;
        input = e.GetEntityComponent<InputComponent>();
        cameraComponent = e.GetEntityComponent<CameraComponent>();
        characterController = e.GetComponent<CharacterController>();
        localSkill = e.GetEntityComponent<LocalSkillComponent>();

        lastSentTick = TickService.Instance.ClientTick;
        velocity = Vector3.zero;

        lastSentMotionState = MotionStateType.Idle;
        lastSentYaw = 0f;
        lastSentDir = Vector3.zero;
    }

    public override void UpdateEntity(float dt)
    {
        

        bool canMove = !entity.FSM.Ctx.LockMove;
        bool canTurn = !entity.FSM.Ctx.LockTurn;

        int currentTick = TickService.Instance.ClientTick;

        Vector2 inputAxis = input?.MoveAxis ?? Vector2.zero;
        if (!canMove) inputAxis = Vector2.zero;

        bool hasInput = inputAxis.sqrMagnitude > 0.01f;

        Vector3 wishDir = Vector3.zero;
        if (hasInput)
        {
            Vector3 camForward = cameraComponent != null
                ? cameraComponent.GetCameraForwardProjected()
                : entity.transform.forward;

            Vector3 camRight = cameraComponent != null
                ? cameraComponent.GetCameraRightProjected()
                : entity.transform.right;

            wishDir = (camForward * inputAxis.y + camRight * inputAxis.x).normalized;
        }
        
        float currentEntityYaw = entity.transform.eulerAngles.y;
        TargetYaw = currentEntityYaw;

        if (canTurn && hasInput && wishDir.sqrMagnitude > 0.000001f)
        {
            float targetYaw = Quaternion.LookRotation(wishDir).eulerAngles.y;
            float turnSpeedDeg = 720f;
            float newYaw = Mathf.MoveTowardsAngle(entity.transform.eulerAngles.y, targetYaw, turnSpeedDeg * dt);
            entity.transform.rotation = Quaternion.Euler(0, newYaw, 0);
            TargetYaw = newYaw;
        }
        
        entity.FSM.Ctx.HasMoveInput = hasInput;
        entity.FSM.Ctx.WishDir = wishDir;
        
        float speed = entity.NetworkEntity.Speed + 1;
        Vector3 targetHorizontalVelocity = hasInput
            ? Vector3.ProjectOnPlane(wishDir, groundNormal).normalized * speed
            : Vector3.zero;

        Vector3 currentHorizontalVel = Vector3.ProjectOnPlane(velocity, Vector3.up);
        currentHorizontalVel = Vector3.MoveTowards(currentHorizontalVel, targetHorizontalVelocity, 30f * dt);

        if (isGrounded) velocity.y = -0.1f;
        else
        {
            velocity.y += GRAVITY * dt;
            velocity.y = Mathf.Max(velocity.y, TERMINAL_VELOCITY);
        }

        velocity = currentHorizontalVel + Vector3.up * velocity.y;
        var motion = velocity * dt + externalMotion;
        externalMotion = Vector3.zero;
        Move(motion);
        
      

        // var snapshot = new Snapshot
        // {
        //     Tick = currentTick,
        //     Pos = entity.transform.position,
        //     Yaw = entity.transform.eulerAngles.y,
        //     Speed = speed,
        //     Dir = wishDir,
        //     MotionState = MotionStateType.Move,
        //     ActionState = actionState,
        // };
        // entity.CurrentSnapshot = snapshot;
        //
        // // ✅ 发包判定：修复“刚赋值就比较”的问题
        // bool motionChanged = motionState != lastSentMotionState;
        // bool yawChanged = Mathf.Abs(Mathf.DeltaAngle(lastSentYaw, snapshot.Yaw)) > 1f;
        // bool dirChanged = (snapshot.Dir - lastSentDir).sqrMagnitude > 0.0004f;
        //
        // if (currentTick > lastSentTick + SEND_EVERY_TICKS || motionChanged || yawChanged || dirChanged)
        // {
        //     SendInput(snapshot.Yaw, wishDir, currentTick);
        //     lastSentTick = currentTick;
        //     lastSentMotionState = motionState;
        //     lastSentYaw = snapshot.Yaw;
        //     lastSentDir = snapshot.Dir;
        // }
        //
        // snapshots.Enqueue(snapshot);
        // while (snapshots.Count > 4096) snapshots.Dequeue();
    }




    public void AddExternalMotion(Vector3 delta)
    {
        externalMotion += delta;
    }

    private void Move(Vector3 motion)
    {
        if (isGrounded && velocity.y < 0) velocity.y = -0.1f;
        characterController.Move(motion);
        isGrounded = characterController.isGrounded;
    }

    private void SendInput(float yaw, Vector3 dir, int tick)
    {
        var packet = new ClientPlayerMove
        {
            ClientTick = tick,
            Position = HelperUtility.Vector3ToShortArray(entity.transform.position),
            Direction = HelperUtility.Vector3ToSbyteArray(dir),
            Yaw = HelperUtility.YawToShort(yaw),
        };
        GameClient.Instance.Send(Protocol.PlayerMove, packet);
    }

    public void ReconcileTo(bool isValid, int ackTick)
    {
        while (snapshots.Count > 0 && snapshots.Peek().Tick <= ackTick)
            snapshots.Dequeue();

        if (isValid || snapshots.Count == 0) return;

        var last = snapshots.Peek();
        entity.transform.position = last.Pos;
        entity.transform.rotation = Quaternion.Euler(0, last.Yaw, 0);
        velocity = Vector3.zero;
    }
}
