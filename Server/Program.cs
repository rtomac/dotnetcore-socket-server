using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Server
{
    class Program
    {
        private static StatusReporter _reporter;
        private static FileStream _file;
        private static QueueingLogWriter _logWriter;
        private static LocalhostSocketListener _listener;

        static void Main(string[] args)
        {
            ConfigureLogging(Level.Info);

            Console.WriteLine("Note: Press 'q' to stop server.");

            var logPath = Path.Combine(Directory.GetCurrentDirectory(), "numbers.log");
            _file = new FileStream(logPath, FileMode.Create);
            _logWriter = new QueueingLogWriter(new StreamWriter(_file, Encoding.ASCII));
            _logWriter.Start();

            _reporter = new StatusReporter();
            _reporter.Start(10);

            _listener = new LocalhostSocketListener(4000, 5);
            _listener.Start(socket =>
            {
                var reader = new SocketStreamReader(socket);
                reader.Read(ProcessValue);
            });

            Console.CancelKeyPress += delegate { StopServer(); };

            while (Console.ReadKey(true).Key != ConsoleKey.Q) { }
            StopServer();
        }

        private static void ProcessValue(int value)
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

        private static void ConfigureLogging(Level level)
        {
            var repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            var appender = new ConsoleAppender
            {
                Layout = new PatternLayout("%message%newline")
            };
            ((Hierarchy)repository).Root.Level = level;
            BasicConfigurator.Configure(repository, appender);
        }

        private static void StopServer()
        {
            Console.WriteLine("Stopping server...");
            try
            {
                _logWriter.Stop();
                _file.Dispose();
                _listener.Stop();
                _reporter.Stop();
            }
            catch
            {
            }
        }
    }
}