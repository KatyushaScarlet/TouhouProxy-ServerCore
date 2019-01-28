﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerCore
{
    public class Model
    {
        public const string Client_Arrive_Handshake = "CAH";
        public const string Server_Proxy_Start = "SPS";
        public const string Game_Data_Forward = "GDF";
        public static byte[] Heartbeat = { 0x0E, 0x03, 0xE4, 0x00, 0x00, 0x00, 0x03, 0x01, 0x00, 0x00 };
        public static byte[] Game_Heartbeat = { 0x01, 0x02, 0x00, 0x2A, 0x30, 0x7F, 0x00, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x02, 0x00, 0x2A, 0x30, 0x7F, 0x00, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00 };

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

        public static bool ByteEquals(byte[] b1, byte[] b2)//debug
        {
            if (b1.Length != b2.Length) return false;
            if (b1 == null || b2 == null) return false;
            for (int i = 0; i < b1.Length; i++)
                if (b1[i] != b2[i])
                    return false;
            return true;
        }

        public static string GetDatetime()
        {
            return DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss");
        }

        public static int GetRandomNumber(int min, int max)//获取随机数（保证随机）
        {
            Random random = new Random(Guid.NewGuid().ToString("N").GetHashCode());
            return random.Next(min, max);
        }

        public static byte[] ByteSplice(byte[] bytes1, byte[] bytes2)//byte[]拼接
        {
            List<byte> list1 = bytes1.ToList();
            list1.AddRange(bytes2);
            byte[] result = list1.ToArray();
            return result;
        }
    }
}
