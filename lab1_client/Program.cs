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

            string name;

            bool result;

            do
            {
                var operation = CommandProcessor.GetRequiredOperation();

                result = operation(socket);

                name = nameof(operation);
            }
            while (!(name.Equals("CLOSE", StringComparison.InvariantCultureIgnoreCase) && result));
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
