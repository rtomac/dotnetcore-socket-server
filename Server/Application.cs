using System;
using System.IO;
using System.Text;

namespace Server
{
    /// <summary>
    /// Class that orchestrates and controls the lifetime of the different
    /// components in the application, including the socket listener, the
    /// log file writer, and the status reporter.
    /// </summary>
    /// <remarks>
    /// This class doesn't directly create/manage threads. The classes
    /// to which it delegates do:
    /// - <see cref="LocalhostSocketListener"/>: Manages background thread
    ///     for server socket, and additional threads for each socket connection.
    /// - <see cref="QueueingLogWriter"/>: Manages background thread that pulls
    ///     values off a queue and writes them to disk.
    /// - <see cref="StatusReporter"/>: Manages background thread that periodically
    ///     writes status to the console.
    /// 
    /// It is critical that instances of this class are disposed of properly.
    /// </remarks>
    public class Application : IDisposable
    {
        private readonly int _statusInterval;
        private readonly StatusReporter _reporter;
        private readonly LocalhostSocketListener _listener;
        private readonly FileStream _logFile;
        private readonly QueueingLogWriter _logWriter;

        public Application(int port, int maxConnections, int statusInterval, string logFilePath)
        {
            _statusInterval = statusInterval;

            _reporter = new StatusReporter();
            _listener = new LocalhostSocketListener(port, maxConnections);

            _logFile = new FileStream(logFilePath, FileMode.Create);
            _logWriter = new QueueingLogWriter(new StreamWriter(_logFile, Encoding.ASCII));
        }

        public void Run(Action terminationCallback = null)
        {
            _reporter.Start(_statusInterval);

            // Start listening on the specified port, and get callback whenever
            // new socket connection is established.
            _listener.Start(socket =>
            {
                // New connection. Start reading data from the network stream.
                // Socket stream reader will call back when a valid value is read
                // and/or when a terminate command is received.
                var reader = new SocketStreamReader(socket);
                reader.Read(ProcessValue, terminationCallback);
            });
        }

        private void ProcessValue(int value)
        {
            // Logger is responsible to write the value to disk. It
            // will tell us if the value was unique, so we can record that
            // correctly with the status reporter.
            if (_logWriter.WriteUnique(value))
            {
                _reporter.RecordUnique();
            }
            else
            {
                _reporter.RecordDuplicate();
            }
        }

        public void Dispose()
        {
            _logWriter.Dispose();
            _logFile.Dispose();

            _listener.Stop();
            _reporter.Stop();
        }
    }
}
