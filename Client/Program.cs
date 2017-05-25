using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, 4000));

            socket.Send(Encoding.ASCII.GetBytes("123456789" + Environment.NewLine));
            socket.Send(Encoding.ASCII.GetBytes("012345678" + Environment.NewLine));
            socket.Send(Encoding.ASCII.GetBytes("terminate" + Environment.NewLine));

            socket.Shutdown(SocketShutdown.Both);
            socket.Dispose();
        }
    }
}