using System;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    /// <summary>
    /// Reads data from a network stream over a socket connection,
    /// processes it, and calls back to event handlers to do something with.
    /// </summary>
    /// <remarks>
    /// This class contains all logic for reading numeric values
    /// from the stream and ensuring that the data is in the correct
    /// format. If it's not, it ceases reading (which would allows
    /// the parent thread to close the socket connection and complete/die).
    /// 
    /// It's also responsible to detect the 'terminate' sequence/command.
    /// 
    /// For performance, bytes are read and processed mathematically to construct
    /// the numbers, rather than transforming them into strings.
    /// 
    /// Server-native new line sequences are properly detected to support
    /// different OS platforms.
    /// </remarks>
    public class SocketStreamReader
    {
        public static int ValueSize => 9;
        public static byte[] NewLineSequence => Encoding.ASCII.GetBytes(Environment.NewLine);
        public static int NewLineSize => NewLineSequence.Length;
        public static int ChunkSize => ValueSize + NewLineSize;

        private static readonly string _terminateSequence = "terminate" + Environment.NewLine;

        private readonly ISocketConnectionProxy _socketConnection;

        public SocketStreamReader(Socket socket) : this(new SocketConnectionProxy(socket))
        {
        }
        public SocketStreamReader(ISocketConnectionProxy socketConnection)
        {
            _socketConnection = socketConnection;
        }

        public void Read(Action<int> valueReadCallback, Action terminationCallback = null)
        {
            var buffer = new byte[ChunkSize];
            int bytesRead;

            // Read data in blocks of known chunk size.
            while ((bytesRead = TryReadChunk(buffer)) == ChunkSize)
            {
                // Convert to 32-bit int. If not valid number, we're done.
                if (!TryConvertToInt32(buffer, out int value))
                {
                    // Check for terminate command. If found, invoke callback
                    // so caller can act on it.
                    if (IsTerminateSequence(buffer))
                    {
                        terminationCallback?.Invoke();
                        break;
                    }
                    break;
                }

                // When we get a good value, invoke callback so value can be processed.
                valueReadCallback?.Invoke(value);
            }
        }

        private int TryReadChunk(byte[] buffer)
        {
            // We can't be sure that we're receiving the full 9+ bytes at the same
            // time, so loop to read data until we fill the buffer. Under normal
            // circumstances, we should, in which case there's just a single
            // Receive call here.

            int bytesRead;
            int bufferOffset = 0;
            while (bufferOffset < buffer.Length)
            {
                bytesRead = _socketConnection.Receive(buffer, bufferOffset, buffer.Length - bufferOffset);
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

            // Make sure chunk correctly terminates with new line sequence.
            // Loop here to support Windows with two-byte sequence.
            for (var i = 0; i < NewLineSize; i++)
            {
                if (buffer[ValueSize + i] != NewLineSequence[i])
                {
                    return false;
                }
            }

            // Read through first 9 bytes and look for numeric digit. Use
            // the proper multiplier for its place and construct the numeric value.
            // If we find a non-numeric char, we short-circuit and return false.
            byte b;
            int place;
            for (var i = 0; i < ValueSize; i++)
            {
                b = buffer[i];
                if (b < 48 || b > 57)
                {
                    return false;
                }
                place = (int)Math.Pow(10, ValueSize - i - 1);
                value += ((b - 48) * place);
            }
            return true;
        }

        private bool IsTerminateSequence(byte[] buffer)
        {
            if (buffer[0] == 84 || buffer[0] == 116) // Check first byte before transforming to string.
            {
                return Encoding.ASCII.GetString(buffer)
                    .Equals(_terminateSequence, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}
