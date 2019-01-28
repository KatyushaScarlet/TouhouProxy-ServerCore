using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    class UdpForwardServer
    {
        private int serverPort = 0;
        private UdpClient udpClient = null;
        //private IPEndPoint player1 = null;
        //private IPEndPoint player2 = null;
        //<编号，客户端>
        private Dictionary<int, IPEndPoint> forwardList = null;

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
            byte[] buffer = udpClient.EndReceive(ar, ref newPlayer);//原始数据

            if (buffer.Length > 0)
            {
                string[] messageArrive = Model.Decode(buffer.Length, buffer);//解码数据
                byte[] messageSend = null;


                //TODO debug

                /*
                 * 游戏数据包转发结构
                 * [Game_Data_Forward][index][原数据]
                 */

                if (forwardList == null)
                {
                    if (messageArrive[0] == Model.Client_Arrive_Handshake)
                    {
                        //收到握手包，记录host player，编号为0
                        //lock (forwardList)
                        //{
                        forwardList = new Dictionary<int, IPEndPoint>();
                        forwardList.Add(0, newPlayer);
                        Console.WriteLine(string.Format("[{0}][INFO]Port [{1}] get host player [{2}]", Model.GetDatetime(),serverPort,newPlayer));
                        //}
                    }
                }
                else if (GetUserIndex(newPlayer) == -1)
                {
                    //记录不存在，添加新客户端
                    lock (forwardList)
                    {
                        //添加索引
                        int index = Model.GetRandomNumber(1, 255);
                        forwardList.Add(index, newPlayer);
                        Console.WriteLine(string.Format("[{0}][INFO]Port [{1}] get guest player [{2}] , now {3} guest player(s)", Model.GetDatetime(), serverPort, newPlayer, forwardList.Count - 1));
                        //转发给host player
                        messageSend = Model.ByteSplice(Model.Encode(Model.Game_Data_Forward, index), buffer);
                        udpClient.Send(messageSend, messageSend.Length, forwardList[0]);
                    }
                }
                else /*if (messageArrive[0] == Model.Game_Data_Forward)//有bug：若数据并非来自转发客户端，则不包含Game_Data_Forward*/
                {
                    //客户端已存在，转发数据
                    if (forwardList[0].Equals(newPlayer))
                    {
                        //数据来自host player，根据数据包内的索引（index）转发给相应的客户端（查forwardList）
                        //TODO：数据格式可能有异常
                        if (messageArrive[0]==Model.Game_Data_Forward)
                        {
                            int index = int.Parse(messageArrive[1]);
                            IPEndPoint ip = forwardList[index];
                            byte[] data = Encoding.UTF8.GetBytes(messageArrive[2]);
                            udpClient.Send(data, data.Length, ip);
                        }
                    }
                    else
                    {
                        //数据来自其他客户端，转发给host player
                        int index = GetUserIndex(newPlayer);
                        messageSend = Model.ByteSplice(Model.Encode(Model.Game_Data_Forward, index), buffer);
                        udpClient.Send(messageSend, messageSend.Length, forwardList[0]);
                    }
                }

                //仅供测试
                //if (Model.ByteEquals(buffer,Model.Heartbeat))
                //{
                //    Console.WriteLine(string.Format("[DBUG]Heartbeat from [{0}] on port [{1}]", newPlayer, serverPort));
                //}

                //if (player1 == null)
                //{
                //    if (messageArrive[0] == Model.Client_Arrive_Handshake)
                //    {
                //        //记录Player1
                //        hostPlayer = newPlayer;
                //        Console.WriteLine(string.Format("[{0}][INFO]Port [{1}] geted Host Player [{2}]",Model.GetDatetime(), serverPort, newPlayer));
                //        //握手包不需要转发
                //    }
                //    //结束
                //}
                //else if (player2 == null)
                //{
                //    if (!newPlayer.Equals(player1))
                //    {
                //        //记录Player2
                //        player2 = newPlayer;
                //        Console.WriteLine(string.Format("[{0}][INFO]Port [{1}] geted Player2 [{2}]", Model.GetDatetime(), serverPort, newPlayer));
                //        udpClient.Send(buffer, buffer.Length, player1);
                //    }
                //    //结束
                //}
                //else if (player1 != null && player2 != null)
                //{
                //    //相互转发
                //    if (newPlayer.Equals(player1))
                //    {
                //        udpClient.Send(buffer, buffer.Length, player2);
                //    }
                //    else if (newPlayer.Equals(player2))
                //    {
                //        udpClient.Send(buffer, buffer.Length, player1);
                //    }
                //    //结束
                //}
            }
            //完成时调用自身
            udpClient.BeginReceive(new AsyncCallback(ReadComplete), null);
        }

        public void Close()//TODO 销毁对象
        {
            flagClose = true;
            udpClient = null;
            //player1 = null;
            //player2 = null;
            forwardList = null;
        }

        private int GetUserIndex(IPEndPoint user)
        {
            //TODO 优化
            int result = -1;
            if (forwardList != null && forwardList.Count > 0)
            {
                foreach (var item in forwardList)
                {
                    if (item.Value.Equals(user))
                    {
                        result = item.Key;
                    }
                }
            }
            return result;

            //因为ContainsValue时间为O(n)，所以弃用
            //if (forwardList.ContainsValue(user))
            //{
            //    //result = (from i in forwardList
            //    //              where i.Value.Equals(iPEndPoint)
            //    //              select i.Key);

            //    result = forwardList.FirstOrDefault(x => x.Value.Equals(user)).Key;
            //}
        }
    }
}
