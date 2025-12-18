using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MessageDispatcher
{
 private class Handler
    {
        public Action<GamePacket> Action; 
        public bool RunOnMainThread;
    }

    private readonly Dictionary<Protocol, List<Handler>> _handlers = new();

    /// <summary>注册处理器（默认在主线程执行，兼容旧签名）</summary>
    public void RegisterHandler(Protocol protocolId, Action<GamePacket> handler) =>
        RegisterHandler(protocolId, handler, runOnMainThread: true);

    /// <summary>注册处理器</summary>
    public void RegisterHandler(Protocol protocolId, Action<GamePacket> handler, bool runOnMainThread)
    {
        if (handler == null) { Debug.LogError($"空处理器: {protocolId}"); return; }
        if (!_handlers.TryGetValue(protocolId, out var list))
        {
            list = new List<Handler>();
            _handlers[protocolId] = list;
        }
        if (list.Exists(h => h.Action == handler)) { Debug.LogWarning($"重复注册: {protocolId}"); return; }
        list.Add(new Handler { Action = handler, RunOnMainThread = runOnMainThread });
    }

    public bool UnregisterHandler(Protocol protocolId, Action<GamePacket> handler = null)
    {
        bool removed = false;
        if (handler == null)
        {
            removed |= _handlers.Remove(protocolId);
        }
        else if (_handlers.TryGetValue(protocolId, out var list))
        {
            removed |= list.RemoveAll(h => h.Action == handler) > 0;
        }
        return removed;
    }

    /// <summary>分发到处理器</summary>
    public void Dispatch(GamePacket packet)
    {
        try
        {
            if (_handlers.TryGetValue((Protocol)packet.ProtocolId, out var list))
            {
                foreach (var h in list)
                {
                    var action = h.Action;
                    if (h.RunOnMainThread) MainThreadDispatcher.Post(() => SafeInvoke(action, packet));
                    else SafeInvoke(action, packet);
                }
            }
            else Debug.LogWarning($"未注册协议ID：{packet.ProtocolId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"分发消息失败：{e}，协议ID：{packet.ProtocolId}");
            throw;
        }
    }

    private static void SafeInvoke(Action<GamePacket> action, GamePacket p)
    {
        try { action(p); } catch (Exception e) { Debug.LogError($"[{p.ProtocolId}]{e}"); }
    }
}
