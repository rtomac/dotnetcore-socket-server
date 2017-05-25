using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Server
{
    class Program
    {
        private static Application _app;
        private static ManualResetEventSlim _exitSignal = new ManualResetEventSlim();

        static void Main(string[] args)
        {
            ConfigureLogging(Level.Info);

            var port = 4000;
            var maxConnections = 5;
            var statusInterval = 10;
            var logFile = "numbers.log";

            var cmd = new CommandLineApplication()
            {
                FullName = "A socket server which will log 9-digit numbers that are sent to it.",
                Name = "dotnet run --"
            };
            var portOption = cmd.Option("-p|--port <port>", $"The port on which the server should run. Default: {port}", CommandOptionType.SingleValue);
            var maxConnectionsOption = cmd.Option("-m|--max <max>", $"The maximum number of socket connections the server should allow. Default: {maxConnections}", CommandOptionType.SingleValue);
            var statusIntervalOption = cmd.Option("-s|--status <interval>", $"The number of seconds between each status report. Default: {statusInterval}", CommandOptionType.SingleValue);
            var logFileOption = cmd.Option("-l|--log <filePath>", $"The path to which value should be logged. Default: {logFile} in the working dir.", CommandOptionType.SingleValue);
            cmd.HelpOption("-?|-h|--help");
            cmd.OnExecute(() =>
            {
                if (portOption.HasValue()) port = int.Parse(portOption.Value());
                if (maxConnectionsOption.HasValue()) maxConnections = int.Parse(maxConnectionsOption.Value());
                if (statusIntervalOption.HasValue()) statusInterval = int.Parse(statusIntervalOption.Value());
                if (logFileOption.HasValue()) logFile = logFileOption.Value();

                Run(port, maxConnections, statusInterval, logFile);

                return 0;
            });
            cmd.Execute(args);
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

        private static void Run(int port, int maxConnections, int statusInterval, string logFile)
        {
            Console.WriteLine("Note: Press <CTRL-C> to stop server.");

            _app = new Application(
                port, maxConnections, statusInterval,
                Path.Combine(Directory.GetCurrentDirectory(), logFile));
            _app.Run(TerminateCommandReceived);

            Console.CancelKeyPress += delegate { StopServer(); };

            _exitSignal.Wait();
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

        private static void TerminateCommandReceived()
        {
            Console.WriteLine("Terminate command received.");
            StopServer();
            _exitSignal.Set();
        }
    }
}