using System;
using System.Text;

namespace Server
{
    public class SocketStreamReader
    {
        public static int ValueSize => 9;
        public static byte[] NewLineSequence => Encoding.ASCII.GetBytes(Environment.NewLine);
        public static int NewLineSize => NewLineSequence.Length;
        public static int ChunkSize => ValueSize + NewLineSize;

        private readonly ISocketConnectionProxy _socketConnectionHandler;
        public SocketStreamReader(ISocketConnectionProxy socketConnectionHandler)
        {
            _socketConnectionHandler = socketConnectionHandler;
        }

        public void Read(Action<int> valueReadCallback)
        {
            var buffer = new byte[ChunkSize];
            int bytesRead;
            while ((bytesRead = TryReadChunk(buffer)) == ChunkSize)
            {
                if (!TryConvertToInt32(buffer, out int value))
                {
                    break;
                }
                valueReadCallback?.Invoke(value);
            }
        }

        private int TryReadChunk(byte[] buffer)
        {
            int bytesRead;
            int bufferOffset = 0;
            while (bufferOffset < buffer.Length)
            {
                bytesRead = _socketConnectionHandler.Receive(buffer, bufferOffset, buffer.Length - bufferOffset);
                if (bytesRead == 0)
                {
                    break;
                }
                bufferOffset += bytesRead;
            }
            return bufferOffset;
        }

        private bool TryConvertToInt32(byte[] buffer, out int value)
        {
            value = 0;
            for (var i = 0; i < NewLineSize; i++)
            {
                if (buffer[ValueSize + i] != NewLineSequence[i])
                {
                    return false;
                }
            }

            var str = Encoding.ASCII.GetString(buffer).Trim();
            return int.TryParse(str, out value);
        }
    }
}
