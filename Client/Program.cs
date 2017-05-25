using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var stopSignal = false;
            var rng = RandomNumberGenerator.Create();
            int max = args.Length > 0 ? int.Parse(args[0]) : 20000;
            int count = 0;

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Console.WriteLine("Connecting to port 4000...");
            socket.Connect(new IPEndPoint(IPAddress.Loopback, 4000));
            Console.WriteLine("Connected.");

            Console.CancelKeyPress += delegate
            {
                stopSignal = true;
                socket.Shutdown(SocketShutdown.Both);
                socket.Dispose();
            };

            var bytes = new byte[4];
            uint value;
            while (!stopSignal && count++ < max)
            {
                rng.GetBytes(bytes);
                value = BitConverter.ToUInt32(bytes, 0);
                socket.Send(Encoding.ASCII.GetBytes(ToPaddedString(value) + Environment.NewLine));
                
                if (count % 10000 == 0)
                {
                    Console.WriteLine($"{count} values sent.");
                }
            }
        }

        private static string ToPaddedString(uint value)
        {
            var str = value.ToString();
            if (str.Length > 9)
            {
                str = str.Substring(0, 9);
            }
            else if (str.Length < 9)
            {
                str = (new String('0', 9 - str.Length)) + str;
            }
            return str;
        }
    }
}