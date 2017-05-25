using log4net;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    /// <summary>
    /// Opens a server socket on the local host bound to the specified port which
    /// listens for incoming connections and dispatches new threads to handle each
    /// of those connections.
    /// </summary>
    /// <remarks>
    /// Manages the lifetime of threads created to handle connections. Keeps
    /// track of the number of connections, and refuses connections past the 
    /// specified threshold.
    /// 
    /// The server socket thread stays alive by virtue of the <c>Accept</c> method.
    /// 
    /// Each connection thread will stay alive as long as the callback in
    /// the <see cref="Application"/> class is processing it. Once that completes,
    /// it will disconnect the client and let the thread die.
    /// 
    /// The only shared resource in this class that needs to be synchronized
    /// is the list of sockets that we use to keep track of the open
    /// connections.
    /// </remarks>
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
            // Start thread for server socket that will listen for
            // new connections.
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
                // Close socket connections (thereby release threads)
                // for each open connection.
                _connections.ForEach(ShutdownSocket);
            }

            if (_socket != null)
            {
                // Close server socket and stop listening on port.
                // Will release the thread on which that's running.
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
                    // Blocking method
                    connection = _socket.Accept();
                }
                catch (SocketException ex)
                {
                    _log.Debug($"Socket accept failed: {ex.Message}");
                    continue;
                }

                if (ShouldRefuseConnection())
                {
                    // We already have the max number of connections.
                    ShutdownSocket(connection);
                    _log.Info("Socket connection refused.");
                    continue;
                }

                _log.Info("Socket connection accepted.");

                DispatchThreadForNewConnection(connection, newSocketConnectionCallback);
            }
        }

        private bool ShouldRefuseConnection()
        {
            lock (_connectionsLock)
            {
                return _connections.Count >= _maxConnections;
            }
        }

        private void DispatchThreadForNewConnection(Socket connection, Action<Socket> newSocketConnectionCallback)
        {
            // Create thread to manage new socket connection.
            // Will stay alive as long as callback is executing.
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
