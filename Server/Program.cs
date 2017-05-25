using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.IO;
using System.Reflection;

namespace Server
{
    class Program
    {
        private static Application _app;

        static void Main(string[] args)
        {
            ConfigureLogging(Level.Info);

            Console.WriteLine("Note: Press 'q' to stop server.");

            _app = new Application(
                4000, 5, 10,
                Path.Combine(Directory.GetCurrentDirectory(), "numbers.log"));
            _app.Run();

            Console.CancelKeyPress += delegate { StopServer(); };

            while (Console.ReadKey(true).Key != ConsoleKey.Q) { }
            StopServer();
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
                _app.Dispose();
            }
            catch { }
        }
    }
}