using System.Collections.Generic;
using UnityEngine;

public class RemoteMoveComponent : BaseComponent
{
    private EntityBase entity;

    private readonly List<Snapshot> snapshotBuffer = new List<Snapshot>(32);

    // 逻辑插值后的目标位置/朝向
    private Vector3 logicPos;
    private float logicYaw;

    // 实际渲染的位置/朝向（加平滑）
    private Vector3 visualPos;
    private float visualYaw;
    private bool initialized;
    private Vector3 lastNetPos;
    private bool hasLastNetPos;

    private const int MAX_BUFFER_SIZE = 64;
    private const int MAX_LEAD_TICKS = 2;

    public override void Attach(EntityBase e)
    {
        entity = e;
        snapshotBuffer.Clear();
        initialized = false;
    }

    public void OnNetUpdate(int serverTick)
    {
        Vector3 pos = entity.NetworkEntity.Position;
        float yaw = entity.NetworkEntity.Yaw;
        Vector3 dir = entity.NetworkEntity.Direction;

        // 推导 motion：用网络两点差（也可以用 dir）
        MotionStateType motion = MotionStateType.Idle;
        if (hasLastNetPos)
        {
            float distSqr = (pos - lastNetPos).sqrMagnitude;
            if (distSqr > 0.0001f) motion = MotionStateType.Move;
        }
        else
        {
            // 第一次没有 last，先用 dir 推导
            if (dir.sqrMagnitude > 0.01f) motion = MotionStateType.Move;
        }
        lastNetPos = pos;
        hasLastNetPos = true;
        
        ActionStateType action = entity.NetworkEntity.Action;

        var snap = new Snapshot
        {
            Tick = serverTick,
            Pos = pos,
            Yaw = yaw,
            Dir = dir,
            Speed = entity.NetworkEntity.Speed,
            MotionState = motion,
            ActionState = action,
        };

        // 插入 buffer（递增顺序）
        if (snapshotBuffer.Count == 0 || serverTick >= snapshotBuffer[^1].Tick)
        {
            snapshotBuffer.Add(snap);
        }
        else
        {
            int i = snapshotBuffer.Count - 1;
            while (i >= 0 && serverTick < snapshotBuffer[i].Tick)
                i--;
            snapshotBuffer.Insert(i + 1, snap);
        }

        if (snapshotBuffer.Count > MAX_BUFFER_SIZE)
        {
            int removeCount = snapshotBuffer.Count - MAX_BUFFER_SIZE;
            snapshotBuffer.RemoveRange(0, removeCount);
        }

        entity.CurrentSnapshot = snap;
    }

    public override void UpdateEntity(float dt)
    {
        if (snapshotBuffer.Count == 0 || entity == null)
            return;

        if (!initialized)
        {
            initialized = true;
            logicPos = entity.transform.position;
            visualPos = logicPos;
            logicYaw = entity.transform.rotation.eulerAngles.y;
            visualYaw = logicYaw;
        }
        
        double renderTick = TickService.Instance.RenderTickExact;

        while (snapshotBuffer.Count >= 2 &&
               snapshotBuffer[1].Tick <= renderTick - MAX_LEAD_TICKS)
        {
            snapshotBuffer.RemoveAt(0);
        }

        if (snapshotBuffer.Count == 0)
            return;
        
        if (renderTick <= snapshotBuffer[0].Tick)
        {
            logicPos = snapshotBuffer[0].Pos;
            logicYaw = snapshotBuffer[0].Yaw;
        }
        else
        {
            int idx = 0;
            while (idx < snapshotBuffer.Count && snapshotBuffer[idx].Tick < renderTick)
                idx++;

            if (idx >= snapshotBuffer.Count)
            {
                logicPos = snapshotBuffer[^1].Pos;
                logicYaw = snapshotBuffer[^1].Yaw;
            }
            else
            {
                var nextSnap = snapshotBuffer[idx];
                var prevSnap = snapshotBuffer[idx - 1];

                int prevTick = prevSnap.Tick;
                int nextTick = nextSnap.Tick;
                int tickDelta = nextTick - prevTick;

                if (tickDelta <= 0)
                {
                    logicPos = nextSnap.Pos;
                    logicYaw = nextSnap.Yaw;
                }
                else
                {
                    if (renderTick >= nextTick)
                        renderTick = nextTick - 0.001;

                    float t = (float)((renderTick - prevTick) / tickDelta);
                    t = Mathf.Clamp01(t);

                    logicPos = Vector3.Lerp(prevSnap.Pos, nextSnap.Pos, t);
                    logicYaw = Mathf.LerpAngle(prevSnap.Yaw, nextSnap.Yaw, t);
                }
            }
        }
        
        float posSharpness = 20f;
        float rotSharpness = 20f;

        float posBlend = 1f - Mathf.Exp(-posSharpness * dt);
        float rotBlend = 1f - Mathf.Exp(-rotSharpness * dt);

        visualPos = Vector3.Lerp(visualPos, logicPos, posBlend);
        visualYaw = Mathf.LerpAngle(visualYaw, logicYaw, rotBlend);

        entity.transform.position = visualPos;
        entity.transform.rotation = Quaternion.Euler(0f, visualYaw, 0f);
    }
}
