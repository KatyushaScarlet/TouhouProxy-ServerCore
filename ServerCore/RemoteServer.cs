using System;
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

            // 打印连接到的客户端信息
            Console.WriteLine(string.Format("[INFO]New user from [{0}]", client.Client.RemoteEndPoint));

            // 获得流
            streamToClient = client.GetStream();
            buffer = new byte[BufferSize];

            // 在构造函数中就开始准备读取
            AsyncCallback callBack = new AsyncCallback(ReadComplete);
            streamToClient.BeginRead(buffer, 0, BufferSize, callBack, null);
        }

        // 再读取完成时进行回调
        private void ReadComplete(IAsyncResult ar)
        {
            int bytesRead = 0;
            try
            {
                lock (streamToClient)
                {
                    bytesRead = streamToClient.EndRead(ar);
                }

                //TODO 客户端断开事件
                if (bytesRead == 0)
                {
                    throw new Exception("User disconnect");
                }

                string[] messageArrive = Model.Decode(bytesRead, buffer);
                Array.Clear(buffer, 0, buffer.Length);        // 清空缓存，避免脏读

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
                    int userPort = GetRandomPort(portReceiveMin, portReceiveMax);
                    UdpForwardServer udpForwardServer = new UdpForwardServer(userPort);//创建转发
                    userList.Add(userEndpoint, udpForwardServer);

                    messageSend = Model.Encode(Model.Server_Proxy_Start, userPort);
                    Console.WriteLine(string.Format("[INFO]New Port [{0}] for user [{1}]", userPort, client.Client.RemoteEndPoint));
                    /*
                     * 服务端返回：
                     * [Server_Proxy_Start][userPort]
                     */

                }
                else if (true)
                {
                    //TODO 没想好干什么
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
                //TODO 异常捕获
                Console.WriteLine(string.Format("[ERRO]User [{0}],Error Info:\n[ERRO]{1}", client.Client.RemoteEndPoint, ex.Message));
                removeForward();
                client.Close();
                //if (streamToClient != null)
                //    streamToClient.Dispose();
                //client.Close();        
            }
        }

        private int GetRandomPort(int min, int max)//获取随机端口
        {
            Random random = new Random(Guid.NewGuid().ToString("N").GetHashCode());
            return random.Next(min, max);
        }

        private void removeForward()//销毁转发
        {
            lock (userList)
            {
                if (userList.ContainsKey(client.Client.RemoteEndPoint))
                {
                    userList[client.Client.RemoteEndPoint].Close();
                    userList[client.Client.RemoteEndPoint] = null;
                    userList.Remove(client.Client.RemoteEndPoint);
                }
            }
        }
    }
}
