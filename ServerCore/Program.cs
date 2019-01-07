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

            Console.WriteLine(string.Format("[INFO]Server is running at port {0}", mainPort));

            while (true)
            {
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
                RemoteClient remoteClient = new RemoteClient(tcpClient);
            }
        }
    }
}
