using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.Reflection;

namespace Server
{
    class Program
    {
        private static StatusReporter _reporter;
        private static Deduplicator _deduper;
        private static LocalhostSocketListener _listener;

        static void Main(string[] args)
        {
            ConfigureLogging(Level.Info);

            Console.WriteLine("Note: Press 'q' to stop server.");

            _deduper = new Deduplicator();

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
            if (_deduper.IsUnique(value))
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
                _listener.Stop();
                _reporter.Stop();
            }
            catch
            {
            }
        }
    }
}