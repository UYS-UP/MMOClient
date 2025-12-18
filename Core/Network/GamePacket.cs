using MessagePack;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;


public class GamePacket
{
    public ushort ProtocolId { get; private set; }
    public ReadOnlyMemory<byte> Payload { get; }
    public int TotalSize => sizeof(ushort) + Payload.Length;

    public GamePacket(ushort protocolId, ReadOnlyMemory<byte> payload)
    {
        ProtocolId = protocolId;
        Payload = payload;
        
    }
    

    /// <summary>
    /// 将数据包序列化到指定的缓冲区写入器
    /// </summary>
    private void Pack(IBufferWriter<byte> writer)
    {
        // 写入数据包总长度（不包括长度字段本身）
        var totalLength = sizeof(ushort) + Payload.Length;

        var span = writer.GetSpan(sizeof(ushort));
        BinaryPrimitives.WriteUInt16BigEndian(span, (ushort)totalLength);
        writer.Advance(sizeof(ushort));

        // 写入协议ID（使用大端字节序）
        span = writer.GetSpan(sizeof(ushort));
        BinaryPrimitives.WriteUInt16BigEndian(span, ProtocolId);
        writer.Advance(sizeof(ushort));


        // 写入有效载荷
        writer.Write(Payload.Span);
    }

    /// <summary>
    /// 序列化数据包
    /// </summary>
    /// <returns>序列化后的数据</returns>
    public ReadOnlyMemory<byte> SerializePacket()
    {
        var writer = new ArrayBufferWriter<byte>();
        // 序列化数据包
        this.Pack(writer);
        return writer.WrittenMemory;
    }



    public T DeSerializePayload<T>()
    {
        return MessagePackSerializer.Deserialize<T>(Payload);
    }
    public static GamePacket CreateEmpty(Protocol protocol)
    {
        return new GamePacket((ushort)protocol, ReadOnlyMemory<byte>.Empty);
    }

    public override string ToString()
    {
        return $"GamePacket[ProtocolId={ProtocolId}, PayloadSize={Payload.Length}]";
    }



}



