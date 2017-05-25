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
        static void Main(string[] args)
        {
            ConfigureLogging(Level.Info);

            Console.WriteLine("Note: Press 'q' to stop server.");

            var listener = new LocalhostSocketListener(4000, 5);
            listener.Start(socket =>
            {
                var reader = new SocketStreamReader(new SocketConnectionProxy(socket));
                reader.Read(num => Console.WriteLine($"received {num}"));
            });

            Console.CancelKeyPress += delegate { StopServer(listener); };

            while (Console.ReadKey(true).Key != ConsoleKey.Q) { }
            StopServer(listener);
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

        private static void StopServer(LocalhostSocketListener listener)
        {
            Console.WriteLine("Stopping server...");
            listener.Stop();
        }
    }
}