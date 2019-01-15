using System;
using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    class UdpForwardServer
    {
        private int port = 0;
        private UdpClient udpClient = null;
        private IPEndPoint player1 = null;
        private IPEndPoint player2 = null;

        public UdpForwardServer(int port)
        {
            this.port = port;
            udpClient = new UdpClient(port);
            Console.WriteLine(string.Format("[INFO]Forward started at port [{0}]", port));
            udpClient.BeginReceive(new AsyncCallback(ReadComplete), null);
        }

        private void ReadComplete(IAsyncResult ar)
        {
            IPEndPoint newPlayer = null;
            byte[] buffer = udpClient.EndReceive(ar, ref newPlayer);

            if (buffer.Length > 0)
            {
                string[] messageArrive = Model.Decode(buffer.Length, buffer);

                if (player1 == null)
                {
                    if (messageArrive[0] == Model.Client_Arrive_Handshake)
                    {
                        //记录Player1
                        player1 = newPlayer;
                        Console.WriteLine(string.Format("[INFO]Port [{0}] geted Player1 [{1}]", port, newPlayer));
                    }
                    //结束
                }
                else if (player2 == null)
                {
                    if (newPlayer != player1)
                    {
                        //记录Player2
                        player2 = newPlayer;
                        Console.WriteLine(string.Format("[INFO]Port [{0}] geted Player2 [{1}]", port, newPlayer));
                    }
                    //结束
                }
                else if (player1 != null && player2 != null)
                {
                    //相互转发
                    if (newPlayer == player1)
                    {
                        udpClient.Send(buffer,buffer.Length, player2);
                    }
                    else if (newPlayer == player2)
                    {
                        udpClient.Send(buffer, buffer.Length, player1);
                    }
                    //结束
                }
            }
            //完成时调用自身
            udpClient.BeginReceive(new AsyncCallback(ReadComplete), null);
        }

        public void Close()
        {
            udpClient = null;
            player1 = null;
            player2 = null;
        }
    }
}
