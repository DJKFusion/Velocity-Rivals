using System;
using System.Collections.Generic;

namespace VelocityRivals
{
    /// <summary>Bounded big-endian length framing, independent of Unity and socket chunk sizes.</summary>
    public sealed class LanFrameBuffer
    {
        public const int MaximumPayload = 4096;
        readonly List<byte> bytes = new List<byte>();
        public void Feed(byte[] source, int count)
        {
            if (source == null || count < 0 || count > source.Length) throw new ArgumentOutOfRangeException(nameof(count));
            if (bytes.Count + count > 65536) throw new InvalidOperationException("Receive buffer limit exceeded.");
            for (int i = 0; i < count; i++) bytes.Add(source[i]);
        }
        public bool TryRead(out byte[] payload)
        {
            payload = null;
            if (bytes.Count < 4) return false;
            int length = bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
            if (length < 1 || length > MaximumPayload) throw new InvalidOperationException("Invalid LAN packet size.");
            if (bytes.Count < length + 4) return false;
            payload = bytes.GetRange(4, length).ToArray();
            bytes.RemoveRange(0, length + 4);
            return true;
        }
        public static byte[] Encode(byte[] payload)
        {
            if (payload == null || payload.Length < 1 || payload.Length > MaximumPayload) throw new ArgumentOutOfRangeException(nameof(payload));
            int length = payload.Length;
            var frame = new byte[length + 4];
            frame[0] = (byte)(length >> 24); frame[1] = (byte)(length >> 16);
            frame[2] = (byte)(length >> 8); frame[3] = (byte)length;
            Array.Copy(payload, 0, frame, 4, length);
            return frame;
        }
    }
}
