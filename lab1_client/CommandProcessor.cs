using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;


namespace SocketClient
{
    public static class CommandProcessor
    {
        public delegate bool CommandHandler(Socket socket);
        public static Dictionary<string, CommandHandler> Commands { get; private set; }
        static CommandProcessor()
        {
            Commands = new Dictionary<string, CommandHandler>();
            Commands.Add("ECHO", Echo);
            Commands.Add("TIME", Time);
            Commands.Add("CLOSE", Close);
            Commands.Add("ISALIVE", IsAlive);
            Commands.Add("DOWNLOAD", Download);
            Commands.Add("UPLOAD", Upload);
        }

        public static CommandHandler GetRequiredOperation()
        {
            foreach (var command in CommandProcessor.Commands)
            {
                Console.WriteLine($"Enter \'{command.Key}\' if you want to use {command.Key} command");
            }

            string operation = Console.ReadLine().Split(' ')[0];

            return CommandProcessor.Commands[operation];
        }

        private static bool Echo(Socket requestHandler)
        {
            byte[] request = Encoding.Unicode.GetBytes("ECHO "+Console.ReadLine());

            requestHandler.Send(request);

            string response = ResponseData(requestHandler);

            if (!string.IsNullOrEmpty(response))
            {
                Console.WriteLine($"Ответ сервера: {response}");
            }
            else
            {
                throw new ArgumentNullException(nameof(response), $"{nameof(response)} cannot be null");
            }

            return true;
        }

        public static string ResponseData(Socket requestHandler)
        {
            var builder = new StringBuilder();

            int bytes = 0;

            byte[] responseData = new byte[ServerSettings.DataBufferLengthInBytes];

            do
            {
                bytes = requestHandler.Receive(responseData);

                builder.Append(Encoding.Unicode.GetString(responseData, 0, bytes));
            }
            while (requestHandler.Available > 0);

            return builder.ToString();
        }

        private static bool Time(Socket requestHandler)
        {
            byte[] request = Encoding.Unicode.GetBytes("TIME");

            requestHandler.Send(request);

            string response = ResponseData(requestHandler);

            if (!string.IsNullOrEmpty(response))
            {
                Console.WriteLine($"Ответ сервера: {response}");
            }
            else
            {
                throw new ArgumentNullException(nameof(response), $"{nameof(response)} cannot be null");
            }

            return true;
        }

        private static bool Close(Socket requestHandler)
        {
            byte[] request = Encoding.Unicode.GetBytes("CLOSE");

            requestHandler.Send(request);

            requestHandler.Shutdown(SocketShutdown.Both);

            requestHandler.Close();

            return true;
        }
        
        private static bool IsAlive(Socket requestHandler)
        {
            byte[] request = Encoding.Unicode.GetBytes("ISALIVE");

            requestHandler.Send(request);

            int bytes = 0;

            byte[] responseData = new byte[ServerSettings.DataBufferLengthInBytes];

            do
            {
                bytes = requestHandler.Receive(responseData);
            }
            while (requestHandler.Available > 0);

            return responseData.Length == 1 && responseData[0] == 1;
        }

        private static bool Download(Socket requestHandler)
        {
            Console.WriteLine("Введите путь к файлу:");

            string path = Console.ReadLine();

            var requestString = "DOWNLOAD " + path;

            byte[] request = Encoding.Unicode.GetBytes(requestString);

            requestHandler.Send(request);

            string response = ResponseData(requestHandler);

            if (!string.IsNullOrEmpty(response))
            {
                Console.WriteLine($"Ответ сервера: {response}");

                if (response.Equals("Передача файла..."))
                {
                    requestHandler.Send(new byte[] { 1 });
                    using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        byte[] fileDataBuffer = new byte[1024];
                        byte[] lengthBuffer = new byte[sizeof(long)];
                        requestHandler.Receive(lengthBuffer, 0, sizeof(long), SocketFlags.None);
                        long fileLengthInBytes = BitConverter.ToInt64(lengthBuffer);

                        int receivedBytesCount = 0;
                        while (receivedBytesCount < fileLengthInBytes)
                        {
                            var receivedBytesOnIteration = requestHandler.Receive(fileDataBuffer);

                            stream.Write(fileDataBuffer, 0, receivedBytesOnIteration);

                            receivedBytesCount += receivedBytesOnIteration;
                        }
                    }


                    return true;
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(response), $"{nameof(response)} cannot be null");
            }

            return false;
        }

        private static bool Upload(Socket requestHandler)
        {
            Console.WriteLine("Введите путь к файлу:");
            string path = Console.ReadLine();
            if (!File.Exists(path))
            {
                throw new ArgumentException("File is not exist", nameof(path));
            }

            byte[] request = Encoding.Unicode.GetBytes("UPLOAD " + path);
            requestHandler.Send(request);

            byte[] response = new byte[ServerSettings.DataBufferLengthInBytes];
            int receiveBytes = requestHandler.Receive(response);

            if (receiveBytes == 1 && response[0] == 1)
            {
                long length = new FileInfo(path).Length;
                byte[] lengthArray = BitConverter.GetBytes(length);

                using (FileStream fin = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    requestHandler.SendFile(path, lengthArray, null, TransmitFileOptions.UseDefaultWorkerThread);
                }

                return true;
            }
            else
            {
                Console.WriteLine(response.ToString());
            }

            return false;
        }
    }
}

