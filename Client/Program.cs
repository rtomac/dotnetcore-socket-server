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

        static int Main(string[] args)
        {
            var port = 4000;
            var numberToSend = 10000000;
            var terminate = false;

            var cmd = new CommandLineApplication()
            {
                FullName = "A console application that will send data to the number server.",
                Name = "dotnet run --"
            };
            var portOption = cmd.Option("-p|--port <port>", $"The port on which the server is running. Default: {port}", CommandOptionType.SingleValue);
            var numberOption = cmd.Option("-n|--number <number>", "The number of values to send to the server. Default: 10M", CommandOptionType.SingleValue);
            var terminateOption = cmd.Option("-t|--terminate", "Send a terminate command after all values are sent to the server.", CommandOptionType.NoValue);
            cmd.HelpOption("-?|-h|--help");
            cmd.OnExecute(() =>
            {
                if (portOption.HasValue()) port = int.Parse(portOption.Value());
                if (numberOption.HasValue()) numberToSend = int.Parse(numberOption.Value());
                terminate = terminateOption.HasValue();

                return Run(port, numberToSend, terminate);
            });
            return cmd.Execute(args);
        }

        private static int Run(int port, int numberToSend, bool terminate)
        {
            if (!ConnectToServer(port))
            {
                return 1;
            }

            Console.CancelKeyPress += delegate
            {
                _stopSignal = true;
                DisconnectFromServer();
            };

            SendData(numberToSend, terminate);
            DisconnectFromServer();

            return 0;
        }

        private static bool ConnectToServer(int port)
        {
            Console.WriteLine($"Connecting to port {port}...");

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _socket.Connect(new IPEndPoint(IPAddress.Loopback, port));
            }
            catch (SocketException)
            {
                Console.WriteLine("Failed to connect to server.");
                return false;
            }

            Console.WriteLine("Connected.");
            return true;
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
                catch (SocketException)
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

        private static void DisconnectFromServer()
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Dispose();
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