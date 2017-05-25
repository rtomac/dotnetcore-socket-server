using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Client
{
    class Program
    {
        private static Socket _socket;
        private static bool _stopSignal;

        static void Main(string[] args)
        {
            var numberToSend = 1000000;
            var terminate = false;

            var cmd = new CommandLineApplication(throwOnUnexpectedArg: false);
            var numberOption = cmd.Option("-n|--number <number>", "The number of values to send to the server. Default is 1M.", CommandOptionType.SingleValue);
            var terminateOption = cmd.Option("-t|--terminate", "Send a terminate command after values are sent to the server.", CommandOptionType.NoValue);
            cmd.HelpOption("-?|-h|--help");
            cmd.OnExecute(() =>
            {
                if (numberOption.HasValue() && int.TryParse(numberOption.Value(), out int numberValue))
                {
                    numberToSend = numberValue;
                }
                terminate = terminateOption.HasValue();
                return 0;
            });
            cmd.Execute(args);

            ConnectToServer(4000);

            Console.CancelKeyPress += delegate
            {
                _stopSignal = true;
                DisconnectFromServer();
            };

            SendData(numberToSend, terminate);
            DisconnectFromServer();
        }

        private static void ConnectToServer(int port)
        {
            Console.WriteLine($"Connecting to port {port}...");
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(new IPEndPoint(IPAddress.Loopback, port));
            Console.WriteLine("Connected.");
        }

        private static void DisconnectFromServer()
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Dispose();
        }

        private static void SendData(int numberToSend, bool terminate)
        {
            var rng = RandomNumberGenerator.Create();
            int count = 0;
            var bytes = new byte[4];
            uint value = 0;

            while (!_stopSignal && count++ < numberToSend)
            {
                rng.GetBytes(bytes);
                value = BitConverter.ToUInt32(bytes, 0);
                try
                {
                    _socket.Send(Encoding.ASCII.GetBytes(ToPaddedString(value) + Environment.NewLine));
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("Socket connection forcibly closed.");
                    break;
                }

                if (count % 100000 == 0)
                {
                    Console.WriteLine($"{count} values sent.");
                }
            }

            if (!_stopSignal && terminate)
            {
                _socket.Send(Encoding.ASCII.GetBytes("terminate" + Environment.NewLine));
                Console.WriteLine($"Terminate command sent.");
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