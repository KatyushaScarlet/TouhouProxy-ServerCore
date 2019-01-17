using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ServerCore
{
    class UdpForwardServer
    {
        private int serverPort = 0;
        private UdpClient udpClient = null;
        private IPEndPoint player1 = null;
        private IPEndPoint player2 = null;

        private bool flagClose = false;

        public UdpForwardServer(int port)
        {
            this.serverPort = port;
            udpClient = new UdpClient(port);
            //解决Windows下UDP报错问题，详见 https://www.cnblogs.com/pasoraku/p/5612105.html
            //在linux上无此问题
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                uint IOC_IN = 0x80000000;
                uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                udpClient.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
            }

            Console.WriteLine(string.Format("[{0}][INFO]Forward started at port [{1}]",Model.GetDatetime(), port));
            //开始接收
            udpClient.BeginReceive(new AsyncCallback(ReadComplete), null);
        }

        private void ReadComplete(IAsyncResult ar)
        {
            if (flagClose)
            {
                return;//销毁转发
            }

            //TODO 异常处理
            IPEndPoint newPlayer = null;
            byte[] buffer = udpClient.EndReceive(ar, ref newPlayer);

            if (buffer.Length > 0)
            {
                string[] messageArrive = Model.Decode(buffer.Length, buffer);

                //仅供测试
                //if (Model.ByteEquals(buffer,Model.Heartbeat))
                //{
                //    Console.WriteLine(string.Format("[DBUG]Heartbeat from [{0}] on port [{1}]", newPlayer, serverPort));
                //}

                if (player1 == null)
                {
                    if (messageArrive[0] == Model.Client_Arrive_Handshake)
                    {
                        //记录Player1
                        player1 = newPlayer;
                        Console.WriteLine(string.Format("[{0}][INFO]Port [{1}] geted Player1 [{2}]",Model.GetDatetime(), serverPort, newPlayer));
                        //握手包不需要转发
                    }
                    //结束
                }
                else if (player2 == null)
                {
                    if (!newPlayer.Equals(player1))
                    {
                        //记录Player2
                        player2 = newPlayer;
                        Console.WriteLine(string.Format("[{0}][INFO]Port [{1}] geted Player2 [{2}]", Model.GetDatetime(), serverPort, newPlayer));
                        udpClient.Send(buffer, buffer.Length, player1);
                    }
                    //结束
                }
                else if (player1 != null && player2 != null)
                {
                    //相互转发
                    if (newPlayer.Equals(player1) )
                    {
                        udpClient.Send(buffer,buffer.Length, player2);
                    }
                    else if (newPlayer.Equals(player2))
                    {
                        udpClient.Send(buffer, buffer.Length, player1);
                    }
                    //结束
                }
            }
            //完成时调用自身
            udpClient.BeginReceive(new AsyncCallback(ReadComplete), null);
        }

        public void Close()//TODO 销毁对象
        {
            flagClose = true;
            udpClient = null;
            player1 = null;
            player2 = null;
        }
    }
}
