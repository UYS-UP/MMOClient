using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class TickService : SingletonMono<TickService>
{
    private const int tickIntervalMs = 20;       // 50Hz
    private const int heartbeatIntervalMs = 200; // 心跳200ms
    private bool tickOffsetInitialized = false;

    public int TickIntervalMs => tickIntervalMs;

    public event Action<float> OnTick;

    public int InterpDelayTicks { get; private set; } = 0;

    // ⚠ 不再由 TickLoop 推动
    public int ClientTick => (int)(ClientTick64 & 0x7fffffff);

    public long ClientTick64
    {
        get
        {
            long now = stopwatch.ElapsedMilliseconds;
            return now / tickIntervalMs;
        }
    }

    // === 平滑估计 ===
    public double estRttMs;
    private double estClockOffsetMs;
    private double estTickOffset;

    private Stopwatch stopwatch;

    private const double ALPHA_SLOW = 0.1;
    private const double ALPHA_FAST = 0.25;

    private long lastOnTickClientTick64 = 0;

    protected override void Awake()
    {
        base.Awake();
        GameClient.Instance.RegisterHandler(Protocol.Heart, OnHeart);

        stopwatch = Stopwatch.StartNew();

        HeartbeatLoop().Forget();
    }

    private void Update()
    {
        long current = ClientTick64;

        while (lastOnTickClientTick64 < current)
        {
            lastOnTickClientTick64++;
            try { OnTick?.Invoke(tickIntervalMs / 1000f); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    private async UniTaskVoid HeartbeatLoop()
    {
        var token = this.GetCancellationTokenOnDestroy();
        while (this != null && this.enabled && !token.IsCancellationRequested)
        {
            SendHeartbeat();
            await UniTask.Delay(heartbeatIntervalMs, ignoreTimeScale: true, cancellationToken: token);
        }
    }

    private void SendHeartbeat()
    {
        var ping = new ClientHeartPing
        {
            ClientUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        GameClient.Instance.Send(Protocol.Heart, ping);
    }

    private void OnHeart(GamePacket packet)
    {
        var pong = packet.DeSerializePayload<ServerHeartPong>();
        long clientRecvUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        double rtt = clientRecvUtcMs - pong.EchoClientUtcMs;
        estRttMs = estRttMs <= 0 ? rtt : (1 - ALPHA_SLOW) * estRttMs + ALPHA_SLOW * rtt;

        double offsetMs = pong.ServerUtcMs - (pong.EchoClientUtcMs + rtt / 2.0);
        estClockOffsetMs = (1 - ALPHA_SLOW) * estClockOffsetMs + ALPHA_SLOW * offsetMs;
        double clientTick = ClientTick64;

        double serverTickEstimate = pong.Tick;
        double rawDelta = serverTickEstimate - clientTick;
        
        if (!tickOffsetInitialized)
        {
            estTickOffset = rawDelta;
            tickOffsetInitialized = true;
        }
        else
        {
            double diff = rawDelta - estTickOffset;
            double maxStep = 1;
            if (diff > maxStep) diff = maxStep;
            if (diff < -maxStep) diff = -maxStep;

            estTickOffset += diff;
        }
        
        // double jitterGuard = 20;
        // int delayTicks = (int)Math.Ceiling((estRttMs * 0.5 + jitterGuard) / tickIntervalMs);
        // InterpDelayTicks = Mathf.Clamp(delayTicks, 3, 10);
    }
    
    private double ServerTickExact => ClientTick64 + estTickOffset;
    public double RenderTickExact => ServerTickExact - InterpDelayTicks;
    public int RenderTick => (int)Math.Max(0, Math.Floor(RenderTickExact));
}
