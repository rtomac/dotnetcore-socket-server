using log4net;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    public class LocalhostSocketListener
    {
        private readonly int _port;
        private readonly int _maxConnections;
        private Socket _socket;
        private List<Socket> _connections;
        private object _connectionsLock;

        private static ILog _log = LogManager.GetLogger(typeof(LocalhostSocketListener));

        public LocalhostSocketListener(int port, int maxConnections)
        {
            _port = port;
            _maxConnections = maxConnections;
            _connections = new List<Socket>();
            _connectionsLock = new object();
        }

        public void Start(Action<Socket> newSocketConnectionCallback)
        {
            var thread = new Thread(new ThreadStart(() =>
            {
                BindAndListen(newSocketConnectionCallback);
            }));
            thread.Start();
        }

        public void Stop()
        {
            lock (_connectionsLock)
            {
                _connections.ForEach(ShutdownSocket);
            }

            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }
        }

        private void BindAndListen(Action<Socket> newSocketConnectionCallback)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(IPAddress.Loopback, _port));
            _socket.Listen(_maxConnections);

            _log.Info($"Listening for socket connections on port {_port}...");

            BlockAndAcceptConnections(newSocketConnectionCallback);
        }

        private void BlockAndAcceptConnections(Action<Socket> newSocketConnectionCallback)
        {
            while (_socket != null)
            {
                Socket connection;
                try
                {
                    connection = _socket.Accept();
                }
                catch (SocketException ex)
                {
                    _log.Debug($"Socket accept failed: {ex.Message}");
                    continue;
                }

                if (ShouldRefuseConnection())
                {
                    ShutdownSocket(connection);
                    _log.Info("Socket connection refused.");
                    continue;
                }

                _log.Info("Socket connection accepted.");

                DispatchThreadForNewConnection(connection, newSocketConnectionCallback);
            }
        }

        private void DispatchThreadForNewConnection(Socket connection, Action<Socket> newSocketConnectionCallback)
        {
            var thread = new Thread(new ThreadStart(() =>
            {
                ExecuteCallback(connection, newSocketConnectionCallback);

                lock (_connectionsLock)
                {
                    _connections.Remove(connection);
                }
            }));
            thread.Start();

            lock (_connectionsLock)
            {
                _connections.Add(connection);
            }
        }

        private static void ExecuteCallback(Socket connection, Action<Socket> newSocketConnectionCallback)
        {
            try
            {
                newSocketConnectionCallback(connection);
            }
            catch (SocketException ex)
            {
                _log.Debug($"Socket connection closed forcibly: {ex.Message}");
            }
            finally
            {
                ShutdownSocket(connection);
                _log.Info("Socket connection closed.");
            }
        }

        private bool ShouldRefuseConnection()
        {
            lock (_connectionsLock)
            {
                return _connections.Count >= _maxConnections;
            }
        }

        private static void ShutdownSocket(Socket socket)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException ex)
            {
                _log.Debug($"Socket could not be shutdown: {ex.Message}");
            }
        }
    }
}
