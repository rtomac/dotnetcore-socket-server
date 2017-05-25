using System.Net.Sockets;

namespace Server
{
    /// <summary>
    /// A implementation of <see cref="ISocketConnectionProxy"/>
    /// which provides access to an underlying <see cref="Socket"/>.
    /// </summary>
    public class SocketConnectionProxy : ISocketConnectionProxy
    {
        private readonly Socket _socket;

        public SocketConnectionProxy(Socket socket)
        {
            _socket = socket;
        }

        public int Receive(byte[] buffer, int offset, int size)
        {
            return _socket.Receive(buffer, offset, size, SocketFlags.None);
        }
    }
}
