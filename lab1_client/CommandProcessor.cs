using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

namespace SocketClient
{
    public static class CommandProcessor
    {
        public static EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(Settings.ServerIP), Settings.PortServer);

        public delegate string CommandHandler(Socket socket);
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

            return CommandProcessor.Commands[operation.ToUpperInvariant()];
        }

        private static string Echo(Socket requestHandler)
        {
            byte[] request = Encoding.Unicode.GetBytes("ECHO " + Console.ReadLine());

            requestHandler.SendTo(request, remoteEndPoint);


            string response = ResponseData(requestHandler);

            if (!string.IsNullOrEmpty(response))
            {
                Console.WriteLine($"Ответ сервера: {response}");
            }
            else
            {
                throw new ArgumentNullException(nameof(response), $"{nameof(response)} cannot be null");
            }

            return "ECHO";
        }

        public static string ResponseData(Socket requestHandler)
        {
            var builder = new StringBuilder();

            int bytes = 0;

            byte[] responseData = new byte[Settings.DataBufferLengthInBytes];

            EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
            do
            {
                do
                {
                    bytes = requestHandler.ReceiveFrom(responseData, ref senderEndPoint);
                } while (!senderEndPoint.Equals(remoteEndPoint));

                builder.Append(Encoding.Unicode.GetString(responseData, 0, bytes));
            }
            while (requestHandler.Available > 0);

            return builder.ToString();
        }

        private static string Time(Socket requestHandler)
        {
            byte[] request = Encoding.Unicode.GetBytes("TIME");

            requestHandler.SendTo(request, remoteEndPoint);

            string response = ResponseData(requestHandler);

            if (!string.IsNullOrEmpty(response))
            {
                Console.WriteLine($"Ответ сервера: {response}");
            }
            else
            {
                throw new ArgumentNullException(nameof(response), $"{nameof(response)} cannot be null");
            }

            return "TIME";
        }

        private static string Close(Socket requestHandler)
        {
            byte[] request = Encoding.Unicode.GetBytes("CLOSE");

            requestHandler.SendTo(request, remoteEndPoint);

            requestHandler.Close();

            return "CLOSE";
        }

        private static string IsAlive(Socket requestHandler)
        {
            byte[] request = Encoding.Unicode.GetBytes("ISALIVE");

            requestHandler.SendTo(request, remoteEndPoint);

            int bytes = 0;

            byte[] responseData = new byte[Settings.DataBufferLengthInBytes];

            EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
            do
            {
                do
                {
                    bytes = requestHandler.ReceiveFrom(responseData, ref senderEndPoint);
                } while (!senderEndPoint.Equals(remoteEndPoint));
            }
            while (requestHandler.Available > 0);

            Console.WriteLine($"ISAlive {responseData.Length == 1 && responseData[0] == 1}");

            return "ISALIVE";
        }

        private static string Download(Socket requestHandler)
        {
            Console.WriteLine("Введите путь к файлу:");

            string path = Console.ReadLine();

            var requestString = "DOWNLOAD " + path;

            byte[] request = Encoding.Unicode.GetBytes(requestString);

            requestHandler.SendTo(request, remoteEndPoint);

            string response = ResponseData(requestHandler);

            if (!string.IsNullOrEmpty(response))
            {
                Console.WriteLine($"Ответ сервера: {response}");


                if (response.Equals("Передача файла..."))
                {
                    var receiveTimeoutMemory = requestHandler.ReceiveTimeout;
                    requestHandler.ReceiveTimeout = 1000;

                    requestHandler.SendTo(new byte[] { 1 }, remoteEndPoint);

                    using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        Stopwatch timer = new Stopwatch();
                        timer.Start();

                        byte[] fileDataBuffer = new byte[Settings.FileBufferLengthInBytes];
                        byte[] lengthBuffer = new byte[sizeof(long)];
                        EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        do
                        {
                            requestHandler.ReceiveFrom(lengthBuffer, 0, sizeof(long), SocketFlags.None, ref senderEndPoint);
                        } while (!senderEndPoint.Equals(remoteEndPoint));
                        long fileLengthInBytes = BitConverter.ToInt64(lengthBuffer);

                        int numberOfChunk = (int)(fileLengthInBytes / 1024);

                        if (fileLengthInBytes % 1024 != 0)
                        {
                            numberOfChunk++;
                        }

                        bool[] mask = new bool[numberOfChunk];

                        int receivedBytesCount = 0;
                        while (receivedBytesCount < fileLengthInBytes)
                        {
                            try
                            {
                                int receivedBytesOnIteration;

                                long offset = ReceiveChunk(requestHandler, fileDataBuffer, out receivedBytesOnIteration);

                                stream.Seek(offset, SeekOrigin.Begin);
                                stream.Write(fileDataBuffer, 0, receivedBytesOnIteration);

                                SendChunkAcknowledge(requestHandler, offset);

                                if (!mask[offset / 1024])
                                {
                                    receivedBytesCount += receivedBytesOnIteration;
                                    mask[offset / 1024] = true;
                                }

                                var temp = receivedBytesCount / (fileLengthInBytes / 100);

                                Console.Write($"Скачано {temp}%\r");
                            }
                            catch(SocketException)
                            {
                                FreeSocketBuffer(requestHandler);
                            }
                        }

                        timer.Stop();
                        var deltaTime = timer.ElapsedMilliseconds;

                        Console.WriteLine($"Скорость передачи {(double)fileLengthInBytes / (1.0 / 1.024 * deltaTime * 1024)} Mb/s");
                    }

                    requestHandler.ReceiveTimeout = receiveTimeoutMemory;
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(response), $"{nameof(response)} cannot be null");
            }

            FreeSocketBuffer(requestHandler);

            return "DOWNLOAD";
        }

        private static string Upload(Socket requestHandler)
        {
            Console.WriteLine("Введите путь к файлу:");
            string path = Console.ReadLine();
            if (!File.Exists(path))
            {
                throw new ArgumentException("File is not exist", nameof(path));
            }

            byte[] request = Encoding.Unicode.GetBytes("UPLOAD " + path);
            requestHandler.Send(request);

            byte[] response = new byte[Settings.DataBufferLengthInBytes];
            int receiveBytes = requestHandler.Receive(response);

            if (receiveBytes == 1 && response[0] == 1)
            {
                long length = new FileInfo(path).Length;
                byte[] lengthArray = BitConverter.GetBytes(length);

                using (FileStream fin = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    requestHandler.SendFile(path, lengthArray, null, TransmitFileOptions.UseDefaultWorkerThread);
                }
            }
            else
            {
                Console.WriteLine(response.ToString());
            }


            return "UPLOAD";
        }

        public static long ReceiveChunk(Socket requestHandler, byte[] chunkBuffer, out int receivedBytesCount)
        {
            long offset;
            var offsetBuffer = new byte[16];
            EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
            do
            {
                requestHandler.ReceiveFrom(offsetBuffer, sizeof(long), SocketFlags.None, ref senderEndPoint);
                offset = BitConverter.ToInt64(offsetBuffer);
            } while (!senderEndPoint.Equals(remoteEndPoint));


            do
            {
                receivedBytesCount = requestHandler.ReceiveFrom(chunkBuffer, ref senderEndPoint);
            } while (!senderEndPoint.Equals(remoteEndPoint));

            return offset;
        }

        public static void SendChunkAcknowledge(Socket requestHandler, long offset)
        {
            requestHandler.SendTo(BitConverter.GetBytes(offset), remoteEndPoint);
        }

        public static void FreeSocketBuffer(Socket requestHandler)
        {
            EndPoint anyIp = new IPEndPoint(IPAddress.Any, 0);
            var dumbBufferForCleaning = new byte[1024];
            while (requestHandler.Available > 0)
            {
                try
                {
                    requestHandler.ReceiveFrom(dumbBufferForCleaning, ref anyIp);
                }
                catch (SocketException)
                {
                }
            }

        }

    }
}

