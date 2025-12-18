// BackgroundNetworkSender.cs
using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

public class BackgroundNetworkSender : IDisposable
{
    private readonly ITransport _transport;
    private readonly ConcurrentQueue<ReadOnlyMemory<byte>> _queue = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly Thread _thread;
    private volatile bool _running;

    public BackgroundNetworkSender(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _thread = new Thread(SendLoop) { IsBackground = true };
    }

    public void Start() { _running = true; _thread.Start(); }
    public void Stop() { _running = false; _signal.Set(); _thread.Join(1000); }

    public void Enqueue(ReadOnlyMemory<byte> data)
    {
        // CustomLog.Debug($"Enqueue: running? {_running}, connected? {_transport.IsConnected}, data: {data.Length}" );
        if (!_running || !_transport.IsConnected) return;
        
        _queue.Enqueue(data);
        _signal.Set();
    }

    private void SendLoop()
    {
        while (_running)
        {

            _signal.WaitOne();

            while (_queue.TryDequeue(out var data))
            {
                try { _transport.SendSync(data); }
                catch (Exception ex)
                {
                    MainThreadDispatcher.Post(() => Debug.LogError($"[BgSender] {ex}"));
                    break;
                }
            }

            if (_queue.IsEmpty) _signal.Reset();

        }
    }
    


    public void Dispose()
    {
        Stop();
        _signal.Dispose();
    }
}
