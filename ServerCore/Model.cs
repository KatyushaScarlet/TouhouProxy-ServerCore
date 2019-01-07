using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    class Model
    {
        public const string Client_Arrive_Handshake = "Client_Arrive_Handshake";
        public const string Client_Second_Handshake = "Client_Second_Handshake";
        public const string Client_Change_Port = "Client_Change_Port";

        public const string Server_New_Port = "Server_New_Port";
        public const string Server_Proxy_Start = "Server_Proxy_Start";

        public static byte[] Encode(params object[] args)
        {
            string output = "";
            for (int i = 0; i < args.Length; i++)
            {
                output += args[i].ToString() + "|";
            }
            return Encoding.UTF8.GetBytes(output);
        }

        public static string[] Decode(int count, byte[] args)
        {
            string input = Encoding.UTF8.GetString(args, 0, count);
            string[] output = input.Split('|');
            return output;
        }
    }
}
