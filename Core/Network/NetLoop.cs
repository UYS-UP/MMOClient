// NetLoop.cs
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public enum SessionState { Disconnected, Connecting, Connected, Authenticated, InGame }

public class NetLoop : INetLoop
{
    private readonly ITransport _transport;
    private readonly ProtocolParser _parser;
    private readonly MessageDispatcher _dispatcher;

    private readonly ConcurrentQueue<GamePacket> _inbound = new();
    private BackgroundNetworkSender _sender;

    private IPEndPoint _lastEndPoint;
    private CancellationTokenSource _cts;
    private int _reconnectRunning = 0;

    public bool IsConnected => _transport.IsConnected;
    public bool AutoReconnect { get; set; } = true;
    public TimeSpan ReconnectInterval { get; set; } = TimeSpan.FromSeconds(3);
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);
    public Protocol HeartbeatProtocolId { get; set; } = Protocol.Heart;

    public event Action<SessionState> OnStateChanged;
    public event Action<Exception> OnError;

    public NetLoop(ITransport transport, ProtocolParser parser, MessageDispatcher dispatcher)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        _transport.OnDataReceived += OnDataReceived;
        _transport.OnConnectionClosed += OnConnectionClosed;
        _transport.OnConnectionError += OnConnectionError;

        _sender = new BackgroundNetworkSender(_transport);
    }



    public void Connect(IPEndPoint endPoint)
    {
        ResetCts();
        _lastEndPoint = endPoint;
        try
        {
            _transport.Connect(endPoint);
            ChangeState(SessionState.Connected);
            _sender.Start();
            StartDispatchLoop().Forget();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            ChangeState(SessionState.Disconnected);
            MaybeReconnect();
        }
    }

    public async UniTask ConnectAsync(IPEndPoint endPoint)
    {
        Connect(endPoint);
        await UniTask.Yield(); // 兼容旧调用
    }

    public void Disconnect()
    {
        _sender.Stop();
        _transport.Disconnect();
        ChangeState(SessionState.Disconnected);
        CancelCts();
    }

    public void EnqueueSend(ReadOnlyMemory<byte> bytes)
    {
        _sender.Enqueue(bytes);
    }

    public void EnqueueSend(GamePacket packet)
    {
        var data = packet.SerializePacket();
        EnqueueSend(data);
    }
    


    private void OnDataReceived(ReadOnlyMemory<byte> data)
    {
        try
        {
            lock (_parser)
            {
                foreach (var pkt in _parser.ParseData(data))
                    _inbound.Enqueue(pkt);
            }
        }
        catch (Exception ex) { OnError?.Invoke(ex); }
    }

    private void OnConnectionClosed()
    {
        ChangeState(SessionState.Disconnected);
    }
    
    private void OnConnectionError(Exception ex)
    {
        OnError?.Invoke(ex);
    }


    private async UniTaskVoid StartDispatchLoop()
    {
        var token = _cts.Token;
        try
        {
            while (!token.IsCancellationRequested)
            {
                while (_inbound.TryDequeue(out var p))
                    _dispatcher.Dispatch(p);
                await UniTask.Delay(1, cancellationToken: token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { OnError?.Invoke(ex); }
    }

    private void MaybeReconnect()
    {
        if (!AutoReconnect || _lastEndPoint == null) return;
        if (Interlocked.Exchange(ref _reconnectRunning, 1) == 1) return;
        ReconnectLoop().Forget();
    }

    private async UniTaskVoid ReconnectLoop()
    {
        try
        {
            while (!IsConnected && AutoReconnect && !_cts.IsCancellationRequested)
            {
                await UniTask.Delay(ReconnectInterval, cancellationToken: _cts.Token);
                Debug.Log($"[Net] Reconnecting to {_lastEndPoint}...");
                try
                {
                    _transport.Connect(_lastEndPoint);
                    ChangeState(SessionState.Connected);
                    _sender.Start();
                    return;
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                }
            }
        }
        finally { Interlocked.Exchange(ref _reconnectRunning, 0); }
    }

    private void ChangeState(SessionState s)
    {
        OnStateChanged?.Invoke(s);
        if (s == SessionState.Disconnected) MaybeReconnect();
    }

    private void ResetCts() { _cts?.Dispose(); _cts = new CancellationTokenSource(); }
    private void CancelCts() { _cts?.Cancel(); }

    public void Dispose()
    {
        Disconnect();
        _transport.OnDataReceived -= OnDataReceived;
        _transport.OnConnectionClosed -= OnConnectionClosed;
        _transport.OnConnectionError -= OnConnectionError;
        _transport?.Dispose();
        _sender?.Dispose();
        _parser?.Dispose();
        _cts?.Dispose();
    }
}