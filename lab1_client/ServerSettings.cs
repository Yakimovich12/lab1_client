
namespace SocketClient
{
    public class ServerSettings
    {
        public static string ServerIP { get; private set; }

        public static int Port { get; private set; }

        public static int DataBufferLengthInBytes { get; private set; }

        public static int SocketReceiveTimeout { get; private set; }
        static ServerSettings()
        {
            ServerIP = /*"25.66.204.93";*/"192.168.0.2";

            Port = 8005;

            DataBufferLengthInBytes = 1024;

            SocketReceiveTimeout = 10000;
        }
    }
}
