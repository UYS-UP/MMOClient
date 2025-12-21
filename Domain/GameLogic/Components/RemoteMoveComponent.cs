using System.Collections.Generic;
using UnityEngine;

public class RemoteMoveComponent : BaseComponent
{
    private EntityBase entity;
    private readonly List<Snapshot> snapshotBuffer = new List<Snapshot>(64);

    private Vector3 visualPos;
    private float visualYaw;
    private bool initialized;

    private const int MAX_BUFFER_SIZE = 64;
    private const int INTERP_DELAY_TICKS = 2;    // 本地低延迟下缩小插值延迟
    private const int EXTRAPOLATE_MAX_TICKS = 3; // 最多外推 ~60ms（@50Hz）
    private const float HARD_SNAP_DIST = 2.0f;   // 大偏差硬贴合
    private const float MICRO_SNAP_DIST = 0.03f; // 微小偏差直接贴合

    public override void Attach(EntityBase e)
    {
        entity = e;
        snapshotBuffer.Clear();
        initialized = false;
    }

    public void OnNetUpdate(int serverTick)
    {
        var snap = new Snapshot
        {
            Tick = serverTick,
            Pos = entity.NetworkEntity.Position,
            Yaw = entity.NetworkEntity.Yaw,
            Dir = entity.NetworkEntity.Direction,
            Speed = entity.NetworkEntity.Speed,
        };

        if (snapshotBuffer.Count == 0 || serverTick >= snapshotBuffer[^1].Tick)
        {
            snapshotBuffer.Add(snap);
        }
        else
        {
            int i = snapshotBuffer.Count - 1;
            while (i >= 0 && serverTick < snapshotBuffer[i].Tick) i--;
            snapshotBuffer.Insert(i + 1, snap);
        }

        if (snapshotBuffer.Count > MAX_BUFFER_SIZE)
        {
            snapshotBuffer.RemoveRange(0, snapshotBuffer.Count - MAX_BUFFER_SIZE);
        }

        if (!initialized && snapshotBuffer.Count > 0)
        {
            initialized = true;
            visualPos = entity.NetworkEntity.Position;
            visualYaw = entity.NetworkEntity.Yaw;
            entity.transform.position = visualPos;
            entity.transform.rotation = Quaternion.Euler(0, visualYaw, 0);
        }
    }

    public override void UpdateEntity(float dt)
    {
        if (snapshotBuffer.Count == 0 || entity == null) return;

        double renderTick = TickService.Instance.RenderTickExact - INTERP_DELAY_TICKS;
   
        while (snapshotBuffer.Count >= 2 && snapshotBuffer[1].Tick <= renderTick)
        {
            snapshotBuffer.RemoveAt(0);
        }

        Snapshot logicSnap = CalculateInterpolation(renderTick);

        float posErrSqr = (visualPos - logicSnap.Pos).sqrMagnitude;
        if (!initialized || posErrSqr > HARD_SNAP_DIST * HARD_SNAP_DIST || posErrSqr < MICRO_SNAP_DIST * MICRO_SNAP_DIST)
        {
            visualPos = logicSnap.Pos;
        }
        else
        {
            visualPos = logicSnap.Pos;
        }

        visualYaw = logicSnap.Yaw;

        entity.transform.position = visualPos;
        entity.transform.rotation = Quaternion.Euler(0, visualYaw, 0);

        SyncToContext(logicSnap);
    }

    private Snapshot CalculateInterpolation(double renderTick)
    {
        if (snapshotBuffer.Count == 1) return snapshotBuffer[0];

        if (renderTick >= snapshotBuffer[^1].Tick)
        {
            var last = snapshotBuffer[^1];
            var prev = snapshotBuffer.Count >= 2 ? snapshotBuffer[^2] : last;

            double dtTicks = System.Math.Min(EXTRAPOLATE_MAX_TICKS, renderTick - last.Tick);
            if (dtTicks <= 0.0001) return last;

            float tickSec = TickService.Instance.TickIntervalMs / 1000f;
            float dtSec = (float)(dtTicks * tickSec);

            Vector3 dir = last.Dir.sqrMagnitude > 1e-6f
                ? last.Dir.normalized
                : (last.Pos != prev.Pos ? (last.Pos - prev.Pos).normalized : Vector3.zero);

            float speed = last.Speed > 0.001f
                ? last.Speed
                : (prev.Tick != last.Tick
                    ? (last.Pos - prev.Pos).magnitude / (((last.Tick - prev.Tick) * tickSec) + 1e-6f)
                    : 0f);

            Vector3 pos = last.Pos + dir * (speed * dtSec);
            float yaw = last.Yaw;

            return new Snapshot
            {
                Tick = (int)renderTick,
                Pos = pos,
                Yaw = yaw,
                Dir = dir,
                Speed = speed,
            };
        }

        Snapshot prevSnap = snapshotBuffer[0];
        Snapshot nextSnap = snapshotBuffer[1];

        for (int i = 0; i < snapshotBuffer.Count - 1; i++)
        {
            if (snapshotBuffer[i].Tick <= renderTick && snapshotBuffer[i + 1].Tick > renderTick)
            {
                prevSnap = snapshotBuffer[i];
                nextSnap = snapshotBuffer[i + 1];
                break;
            }
        }

        double total = nextSnap.Tick - prevSnap.Tick;
        float t = 0f;
        if (total > 0.0001)
        {
            t = (float)((renderTick - prevSnap.Tick) / total);
        }
        t = Mathf.Clamp01(t);

        return new Snapshot
        {
            Tick = (int)renderTick,
            Pos = Vector3.Lerp(prevSnap.Pos, nextSnap.Pos, t),
            Yaw = Mathf.LerpAngle(prevSnap.Yaw, nextSnap.Yaw, t),
            Dir = Vector3.Lerp(prevSnap.Dir, nextSnap.Dir, t),
            Speed = Mathf.Lerp(prevSnap.Speed, nextSnap.Speed, t),
        };
    }

    private void SyncToContext(Snapshot snap)
    {
        var ctx = entity.FSM.Ctx;
        ctx.HasMoveInput = snap.Dir.sqrMagnitude > 0.001f;
    }
}
