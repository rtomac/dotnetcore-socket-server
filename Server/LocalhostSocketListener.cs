using log4net;
using System;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    public class LocalhostSocketListener
    {
        private readonly int _port;
        private readonly int _maxConnections;
        private static ILog _log = LogManager.GetLogger(typeof(LocalhostSocketListener));

        public LocalhostSocketListener(int port, int maxConnections)
        {
            _port = port;
            _maxConnections = maxConnections;
        }

        public void Listen(Action<Socket> newSocketConnectionCallback)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, _port));
            socket.Listen(_maxConnections);

            while (true)
            {
                _log.Info($"Waiting for a socket connection on port {_port}...");
                var socketConnection = socket.Accept();

                _log.Info($"Socket connection created on port {_port}.");
                newSocketConnectionCallback(socketConnection);
            }
        }
    }
}
