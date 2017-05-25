using System;
using System.Net.Sockets;

namespace Server
{
    public class SocketConnectionHandler : ISocketConnectionHandler
    {
        private readonly Socket _socket;

        public SocketConnectionHandler(Socket socket)
        {
            _socket = socket;
        }

        public int Receive(byte[] buffer, int offset, int size)
        {
            return _socket.Receive(buffer, offset, size, SocketFlags.None);
        }
    }
}
