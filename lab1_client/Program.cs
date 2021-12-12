using System;
using System.Net;
using System.Net.Sockets;

namespace SocketClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var socket = ConfigureSocket();

            Console.WriteLine("Клиент подключен к серверу");

            string operationName;

            do
            {
                var operation = CommandProcessor.GetRequiredOperation();

                operationName = operation(socket);
            }
            while (!operationName.Equals("CLOSE", StringComparison.InvariantCultureIgnoreCase));
        }

        public static Socket ConfigureSocket()
        {
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ServerSettings.ServerIP), ServerSettings.Port);

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.Connect(ipPoint);

            //socket.ReceiveTimeout = ServerSettings.SocketReceiveTimeout;

            return socket;
        }
    }
}
