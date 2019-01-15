using System;
using System.Text;

namespace ServerCore
{
    class Model
    {
        public const string Client_Arrive_Handshake = "CAH";
        public const string Server_Proxy_Start = "SPS";

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
