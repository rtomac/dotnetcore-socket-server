using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using System;
using System.Reflection;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            var appender = new ConsoleAppender
            {
                Layout = new PatternLayout("%5level [%thread]: %message%newline")
            };
            BasicConfigurator.Configure(repository, appender);

            var localhostSocketListener = new LocalhostSocketListener(4000, 5);

            localhostSocketListener.Listen(socket =>
            {
                using (var handler = new SocketConnectionHandler(socket))
                {
                    var reader = new SocketStreamReader(handler);
                    reader.Read(num => Console.WriteLine($"received {num}"));
                }
            });
        }
    }
}