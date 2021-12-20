
namespace SocketClient
{
    public class Settings
    {
        public static string ServerIP { get; private set; }
        public static string ClientIP { get; private set; }
        public static int PortServer { get; private set; }
        public static int PortClient { get; private set; }
        public static int DataBufferLengthInBytes { get; private set; }
        public static int FileBufferLengthInBytes { get; }
        public static int SocketReceiveTimeout { get; private set; }

        static Settings()
        {
            ServerIP = /*"25.66.204.93";*/"192.168.0.2";
            ClientIP = "192.168.0.13"/*"127.0.0.1"*/;
            PortServer = 8005;
            PortClient = 8006;
            DataBufferLengthInBytes = 1024;
            FileBufferLengthInBytes = 1024;
            SocketReceiveTimeout = 10000;
        }
    }
}
