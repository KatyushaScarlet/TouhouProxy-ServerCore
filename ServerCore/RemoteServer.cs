﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    public class RemoteServer
    {
        //客户端
        private TcpClient client;
        private NetworkStream streamToClient;
        //缓冲区
        private const int BufferSize = 1024;
        private byte[] buffer;
        //给用户分配的端口范围
        public const int portReceiveMin = 30000;
        public const int portReceiveMax = 60000;
        //<客户端，转发类>
        private static Dictionary<EndPoint, UdpForwardServer> userList = new Dictionary<EndPoint, UdpForwardServer>();

        public RemoteServer(TcpClient client)
        {
            this.client = client;

            //打印连接到的客户端信息
            Console.WriteLine(string.Format("[{0}][INFO]New user from [{1}]",Model.GetDatetime(), client.Client.RemoteEndPoint));

            //获得流
            streamToClient = client.GetStream();
            buffer = new byte[BufferSize];

            //构造函数中就开始准备读取
            AsyncCallback callBack = new AsyncCallback(ReadComplete);
            streamToClient.BeginRead(buffer, 0, BufferSize, callBack, null);
        }

        //再读取完成时进行回调
        private void ReadComplete(IAsyncResult ar)
        {
            int bytesRead = 0;
            try
            {
                lock (streamToClient)
                {
                    bytesRead = streamToClient.EndRead(ar);
                }

                if (bytesRead == 0)
                {
                    throw new Exception("User disconnect");
                }

                string[] messageArrive = Model.Decode(bytesRead, buffer);
                Array.Clear(buffer, 0, buffer.Length);//清空缓存，避免脏读

                byte[] messageSend = null;

                if (messageArrive[0] == Model.Client_Arrive_Handshake)
                {
                    /*
                     * 新用户到达，格式：
                     * [Client_Arrive_Haneshake]
                     */
                    EndPoint userEndpoint = client.Client.RemoteEndPoint;

                    //删除之间的转发记录（如果有）
                    removeForward();

                    //分配端口
                    int userPort = Model.GetRandomNumber(portReceiveMin, portReceiveMax);
                    UdpForwardServer udpForwardServer = new UdpForwardServer(userPort);//创建转发
                    userList.Add(userEndpoint, udpForwardServer);

                    messageSend = Model.Encode(Model.Server_Proxy_Start, userPort);
                    Console.WriteLine(string.Format("[{0}][INFO]New Port [{1}] for user [{2}]",Model.GetDatetime(), userPort, client.Client.RemoteEndPoint));
                    /*
                     * 服务端返回：
                     * [Server_Proxy_Start][userPort]
                     */

                }

                streamToClient.Write(messageSend, 0, messageSend.Length);
                streamToClient.Flush();

                // 再次调用BeginRead()，完成时调用自身，形成无限循环
                lock (streamToClient)
                {
                    AsyncCallback callBack = new AsyncCallback(ReadComplete);
                    streamToClient.BeginRead(buffer, 0, BufferSize, callBack, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[{0}][ERRO]User [{1}],Error Info:\n[{0}][ERRO]{2}", Model.GetDatetime(), client.Client.RemoteEndPoint, ex.Message));
                removeForward();
                client.Close();
            }
        }

        private void removeForward()//销毁转发
        {
            lock (userList)
            {
                if (userList.ContainsKey(client.Client.RemoteEndPoint))
                {
                    if (userList[client.Client.RemoteEndPoint]!=null)
                    {
                        userList[client.Client.RemoteEndPoint].Close();
                        userList[client.Client.RemoteEndPoint] = null;
                    }
                    userList.Remove(client.Client.RemoteEndPoint);
                }
            }
        }
    }
}
