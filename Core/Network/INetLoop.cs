using System;
using System.Net;
using Cysharp.Threading.Tasks;


public interface INetLoop : IDisposable
{
    bool IsConnected { get; }
    bool AutoReconnect { get; set; }
    Protocol HeartbeatProtocolId { get; set; }
    TimeSpan HeartbeatInterval { get; set; }
    TimeSpan ReconnectInterval { get; set; }
    
    UniTask ConnectAsync(IPEndPoint endPoint);
    void Disconnect();

    void EnqueueSend(ReadOnlyMemory<byte> bytes);

    void EnqueueSend(GamePacket packet);
    
    event Action<SessionState> OnStateChanged;
    event Action<Exception> OnError;
}
