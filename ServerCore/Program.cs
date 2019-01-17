using System;
using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    class Program
    {
        //主端口
        public const int mainPort = 20000;

        static void Main(string[] args)
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, mainPort);
            tcpListener.Start();

            Console.Clear();
            Console.WriteLine(string.Format("[{0}][INFO]Server is running at port {1}",Model.GetDatetime(), mainPort));

            while (true)
            {
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
                RemoteServer remoteClient = new RemoteServer(tcpClient);
            }
        }
    }
}
