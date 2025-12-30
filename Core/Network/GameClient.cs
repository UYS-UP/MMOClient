using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity;
using MessagePack.Unity.Extension;
using UnityEngine;

public class GameClient : SingletonMono<GameClient>
{
   [SerializeField] private string accountId;
   [SerializeField] private string host = "127.0.0.1";
   [SerializeField] private int port = 9999;

   private INetLoop net;
   private MessageDispatcher dispatcher;

   protected override void Awake()
   {
      base.Awake();
      
      var resolver = CompositeResolver.Create(
         UnityBlitWithPrimitiveArrayResolver.Instance, 
         UnityResolver.Instance,
         StandardResolver.Instance
      );
    
      MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
      
      dispatcher = new MessageDispatcher();
      var transport = new TcpTransport();
      var parser = new ProtocolParser();
      net = new NetLoop(transport, parser, dispatcher)
      {
         AutoReconnect = true,
         ReconnectInterval = TimeSpan.FromSeconds(3),
         HeartbeatInterval = TimeSpan.FromSeconds(5)
      };

      net.OnStateChanged += s => CustomLog.Debug($"Session : {s}");
      net.OnError += e => CustomLog.LogError($"Net error : {e?.Message}");
   }
   
   public void Send<T>(Protocol protocolId, T payload)
   {
      // CustomLog.Debug("Send");
      try
      {
         var packet = CreateGamePacket(protocolId, payload);
         net.EnqueueSend(packet); // 注意：如果上面抛了，这里 packet 是 null！
         // CustomLog.Debug("Send success, payload length: " + packet.Payload.Length);
      }
      catch (Exception ex)
      {
         CustomLog.LogError("MessagePack Serialize FAILED: " + ex);
      }
      
   }
      
   private GamePacket CreateGamePacket<T>(Protocol protocol, T data)
   {
      return new GamePacket((ushort)protocol, MessagePackSerializer.Serialize(data));
   }
   
   public async UniTask Connect()
      => await net.ConnectAsync(new IPEndPoint(System.Net.IPAddress.Parse(host), port));

   public void Disconnect() => net.Disconnect();

   // 默认在主线程执行（兼容旧行为）；可选参数切换为后台执行
   public void RegisterHandler(Protocol protocolId, Action<GamePacket> handler, bool runOnMainThread = true)
      => dispatcher?.RegisterHandler(protocolId, handler, runOnMainThread);

   public void UnregisterHandler(Protocol protocolId)
      => dispatcher?.UnregisterHandler(protocolId);

   protected override void OnDestroy()
   {
      base.OnDestroy();
      net?.Dispose();
   }
}
