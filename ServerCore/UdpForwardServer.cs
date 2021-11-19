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

            IPEndPoint newPlayer = null;
            byte[] buffer = udpClient.EndReceive(ar, ref newPlayer);//原始数据

            if (buffer.Length > 0)
            {
                string[] messageArrive = Model.Decode(buffer.Length, buffer);//解码数据
                byte[] messageSend = null;

                /*
                 * 游戏数据包转发结构
                 * [Game_Data_Forward][index][原数据]
                 */

                if (forwardList == null)
                {
                    if (messageArrive[0] == Model.Client_Arrive_Handshake)
                    {
                        forwardList = new Dictionary<int, IPEndPoint>();
                        forwardList.Add(0, newPlayer);
                        Console.WriteLine(string.Format("[{0}][INFO]Port [{1}] get host player [{2}]", Model.GetDatetime(),serverPort,newPlayer));
                    }
                }
                else if (GetUserIndex(newPlayer) == -1)
                {
                    //记录不存在，添加新客户端
                    lock (forwardList)
                    {
                        //添加索引
                        int index = Model.GetRandomNumber(1, 1000);
                        forwardList.Add(index, newPlayer);
                        Console.WriteLine(string.Format("[{0}][INFO]Port [{1}] get new endpoint [{2}] , now {3} endpoint(s)", Model.GetDatetime(), serverPort, newPlayer, forwardList.Count - 1));
                        //转发给host player
                        messageSend = Model.ByteSplice(Model.Encode(Model.Game_Data_Forward, string.Format("{0:0000}", index)), buffer);//序号格式化为4位，在后方带上buffer
                        udpClient.Send(messageSend, messageSend.Length, forwardList[0]);
                    }
                }
                else /*if (messageArrive[0] == Model.Game_Data_Forward)//若数据并非来自转发客户端，则不包含Game_Data_Forward*/
                {
                    //客户端已存在，转发数据
                    if (forwardList[0].Equals(newPlayer))
                    {
                        //数据来自host player，根据数据包内的索引（index）转发给相应的客户端（查forwardList）
                        if (messageArrive[0]==Model.Game_Data_Forward)
                        {
                            int index = int.Parse(messageArrive[1]);
                            IPEndPoint ip = forwardList[index];
                            byte[] message = Model.ByteSplit(buffer, 9);//直接截取buffer位置
                            udpClient.Send(message, message.Length, ip);
                        }
                    }
                    else
                    {
                        //数据来自其他客户端，转发给host player
                        int index = GetUserIndex(newPlayer);
                        messageSend = Model.ByteSplice(Model.Encode(Model.Game_Data_Forward, string.Format("{0:0000}", index)), buffer);//序号格式化为4位，在后方带上buffer
                        udpClient.Send(messageSend, messageSend.Length, forwardList[0]);
                    }
                }
            }
            //完成时调用自身
            udpClient.BeginReceive(new AsyncCallback(ReadComplete), null);
        }

        public void Close()
        {
            flagClose = true;
            udpClient = null;
            forwardList = null;
        }

        private int GetUserIndex(IPEndPoint user)
        {
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
        }
    }
}
