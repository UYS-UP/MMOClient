using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TcpTransport : ITransport
{
    private TcpClient _client;
    private NetworkStream _stream;
    private Thread _receiveThread;
    private volatile bool _running;

    public bool IsConnected => _client?.Connected == true;

    public event Action<ReadOnlyMemory<byte>> OnDataReceived;
    public event Action OnConnectionClosed;
    public event Action<Exception> OnConnectionError;

    public void Connect(IPEndPoint endPoint)
    {
        try
        {
            _client = new TcpClient { NoDelay = true };
            var connectTask = _client.ConnectAsync(endPoint.Address, endPoint.Port);
            if (!connectTask.Wait(5000))
                throw new TimeoutException("Connection timeout");

            _stream = _client.GetStream();
            _running = true;

            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();

            Debug.Log("[PureTcp] Connected to server");
        }
        catch (Exception ex)
        {
            MainThreadDispatcher.Post(() => OnConnectionError?.Invoke(ex));
            throw;
        }
    }

    public void SendSync(ReadOnlyMemory<byte> data)
    {
        if (!_running || _stream == null) return;
        try
        {
            _stream.Write(data.Span);
            _stream.Flush();
        }
        catch (Exception ex)
        {
            MainThreadDispatcher.Post(() => OnConnectionError?.Invoke(ex));
            throw;
        }
    }

    public void SendAsync(ReadOnlyMemory<byte> data)
    {
        // 主线程调用，切到后台发送
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try { SendSync(data); }
            catch (Exception ex)
            {
                MainThreadDispatcher.Post(() => OnConnectionError?.Invoke(ex));
            }
        });
    }

    private void ReceiveLoop()
    {
        var buffer = new byte[16384];
        try
        {
            while (_running && _client.Connected)
            {
                int read = _stream.Read(buffer, 0, buffer.Length);
                if (read == 0) break;

                var packet = new byte[read];
                Buffer.BlockCopy(buffer, 0, packet, 0, read);

                MainThreadDispatcher.Post(() =>
                {
                    try { OnDataReceived?.Invoke(packet); }
                    catch (Exception ex) { Debug.LogException(ex); }
                });
            }
        }
        catch (Exception ex)
        {
            if (_running)
                MainThreadDispatcher.Post(() => OnConnectionError?.Invoke(ex));
        }
        finally
        {
            _running = false;
            MainThreadDispatcher.Post(() =>
            {
                OnConnectionClosed?.Invoke();
                Debug.Log("[PureTcp] Connection closed");
            });
        }
    }

    public void Disconnect()
    {
        _running = false;
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
        if (_receiveThread != null && _receiveThread.IsAlive)
        {
            if (!_receiveThread.Join(3000)) // 延长等待时间
            {
                try { _receiveThread.Interrupt(); } catch { }
            }
        }
    }

    public void Dispose()
    {
        
        Disconnect();
        _client?.Dispose();
    }
}