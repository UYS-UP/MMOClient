using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ITransport : IDisposable
{
    bool IsConnected { get; }

    event Action<ReadOnlyMemory<byte>> OnDataReceived;
    event Action OnConnectionClosed;
    event Action<Exception> OnConnectionError;

    void Connect(IPEndPoint endPoint);
    void Disconnect();
    void SendSync(ReadOnlyMemory<byte> data);  
}