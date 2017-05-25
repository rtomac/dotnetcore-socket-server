using log4net;
using System;
using System.IO;
using System.Text;

namespace Server
{
    public class Application : IDisposable
    {
        private readonly int _statusInterval;
        private readonly StatusReporter _reporter;
        private readonly FileStream _logFile;
        private readonly QueueingLogWriter _logWriter;
        private readonly LocalhostSocketListener _listener;

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

            _listener.Start(socket =>
            {
                var reader = new SocketStreamReader(socket);
                reader.Read(ProcessValue, terminationCallback);
            });
        }

        private void ProcessValue(int value)
        {
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
