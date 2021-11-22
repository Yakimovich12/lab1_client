using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            ServerIP = "192.168.0.2";

            Port = 8005;

            DataBufferLengthInBytes = 256;

            SocketReceiveTimeout = 10000;
        }
    }
}
