using System;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
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