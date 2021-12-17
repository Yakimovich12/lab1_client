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
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ipEndpoint = new IPEndPoint(IPAddress.Parse(Settings.ClientIP), Settings.PortClient);
            socket.Bind(ipEndpoint);

            //socket.ReceiveTimeout = ServerSettings.SocketReceiveTimeout;

            return socket;
        }
    }
}
