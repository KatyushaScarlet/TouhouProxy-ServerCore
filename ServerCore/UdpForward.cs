using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    class UdpForward
    {
        //用户端口
        private int port = 0;

        public UdpForward(int port)
        {
            this.port = port;
            Console.WriteLine(string.Format("[INFO]Forward started at port [{0}]", port));
        }
    }
}
