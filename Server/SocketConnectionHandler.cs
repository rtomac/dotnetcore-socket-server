using System;
using System.Net.Sockets;

namespace Server
{
    public class SocketConnectionHandler : ISocketConnectionHandler, IDisposable
    {
        private readonly Socket _socket;
        private bool _disposed = false;

        public SocketConnectionHandler(Socket socket)
        {
            _socket = socket;
        }

        public int Receive(byte[] buffer, int offset, int size)
        {
            return _socket.Receive(buffer, offset, size, SocketFlags.None);
        }
        
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
