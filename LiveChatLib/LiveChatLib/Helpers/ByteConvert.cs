using System;
using System.Collections.Generic;
using System.Text;

namespace LiveChatLib.Helpers
{
    public static class ByteConvert
    {
        public static byte[] ToByteArray(this short num, bool isBigEndian = false)
        {
            var result = BitConverter.GetBytes(num);
            if (BitConverter.IsLittleEndian && isBigEndian)
            {
                Array.Reverse(result);
            }
            return result;
        }

        public static byte[] ToByteArray(this int num, bool isBigEndian = false)
        {
            var result = BitConverter.GetBytes(num);
            if (BitConverter.IsLittleEndian && isBigEndian)
            {
                Array.Reverse(result);
            }
            return result;
        }

        public static byte[] ToByteArray(this long num, bool isBigEndian = false)
        {
            var result = BitConverter.GetBytes(num);
            if (BitConverter.IsLittleEndian && isBigEndian)
            {
                Array.Reverse(result);
            }
            return result;
        }

        public static short ByteToInt16(this byte[] buffer, bool isBigEndian = false)
        {
            var copy = new byte[buffer.Length];
            Array.Copy(buffer, copy, buffer.Length);
            if(!(BitConverter.IsLittleEndian ^ isBigEndian))
            {
                Array.Reverse(copy);
            }
            var result = BitConverter.ToInt16(copy, 0);
            return result;
        }

        public static int ByteToInt32(this byte[] buffer, bool isBigEndian = false)
        {
            var copy = new byte[buffer.Length];
            Array.Copy(buffer, copy, buffer.Length);
            if (!(BitConverter.IsLittleEndian ^ isBigEndian))
            {
                Array.Reverse(copy);
            }
            var result = BitConverter.ToInt32(copy, 0);
            return result;
        }

        public static long ByteToInt64(this byte[] buffer, bool isBigEndian = false)
        {
            var copy = new byte[buffer.Length];
            Array.Copy(buffer, copy, buffer.Length);
            if (!(BitConverter.IsLittleEndian ^ isBigEndian))
            {
                Array.Reverse(copy);
            }
            var result = BitConverter.ToInt64(copy, 0);
            return result;
        }
    }
}
