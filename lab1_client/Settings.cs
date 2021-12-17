
namespace SocketClient
{
    public class Settings
    {
        public static string ServerIP { get; private set; }
        public static string ClientIP { get; private set; }
        public static int PortServer { get; private set; }
        public static int PortClient { get; private set; }
        public static int DataBufferLengthInBytes { get; private set; }
        public static int SocketReceiveTimeout { get; private set; }

        static Settings()
        {
            ServerIP = /*"25.66.204.93";*/"192.168.0.2";
            ClientIP = "192.168.0.13";
            PortServer = 8005;
            PortClient = 8005;
            DataBufferLengthInBytes = 256;
            SocketReceiveTimeout = 10000;
        }
    }
}
