using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ProtocolParser : IDisposable
{
    private byte[] buffer;
    private int start = 0;      // 有效数据起始
    private int end = 0;        // 有效数据结束（开区间）
    private int expectedLength = -1;

    public ProtocolParser(int initialCapacity = 65536)
    {
        buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
    }

    public IEnumerable<GamePacket> ParseData(ReadOnlyMemory<byte> data)
    {
        EnsureCapacity(data.Length);
        data.Span.CopyTo(buffer.AsSpan(end));
        end += data.Length;

        while (true)
        {
            // 1. 读取包长度
            if (expectedLength < 0)
            {
                if (end - start < 2) break;
                expectedLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(start, 2));
                if (expectedLength <= 2 || expectedLength > 64 * 1024)
                {
                    // Debug.Log($"[ProtocolParser]无效包");
                    Reset();
                    yield break;
                }
                start += 2;
            }

            // 2. 是否有完整包
            if (end - start < expectedLength) break;

            // 3. 读取协议ID
            ushort protocolId = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(start, 2));
            start += 2;
            int payloadSize = expectedLength - 2;
            
            var payloadSpan = buffer.AsSpan(start, payloadSize);
            byte[] copy = payloadSpan.ToArray();
            var payload = copy;

            start += payloadSize;
            expectedLength = -1;
            // Debug.Log($"[Parser] Yield pkt {protocolId}, buffer status: {GetBufferStatus()}");
            yield return new GamePacket(protocolId, payload);
        }

        Compact();
    }

    private void EnsureCapacity(int incoming)
    {
        if (buffer.Length - end >= incoming) return;

        int needed = (end - start) + incoming;
        int newSize = buffer.Length;
        while (newSize < needed) newSize *= 2;

        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        Buffer.BlockCopy(buffer, start, newBuffer, 0, end - start);
        ArrayPool<byte>.Shared.Return(buffer);
        end -= start;
        start = 0;
        buffer = newBuffer;
    }

    private void Compact()
    {
        if (start == 0) return;
        Buffer.BlockCopy(buffer, start, buffer, 0, end - start);
        end -= start;
        start = 0;
    }

    private void Reset()
    {
        start = end = 0;
        expectedLength = -1;
    }

    public void Dispose()
    {
        if (buffer != null)
            ArrayPool<byte>.Shared.Return(buffer);
    }

    public string GetBufferStatus()
        => $"Start={start}, End={end}, Buffer={buffer.Length}, Expected={expectedLength}";
}