using System;
using System.Collections.Generic;
using System.Linq;
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

        public static string DisplayBytes(this byte[] buffer)
        {
            var start = 0;
            var end = buffer.Length < 16 ? buffer.Length : 16;
            var line = 0;
            var sb = new StringBuilder();
            while(start < buffer.Length)
            {
                var part = buffer.Skip(start).Take(end - start);
                sb.AppendLine($"0x{line:X8} {string.Join(" ", part.Select(x => x.ToString("X2")))}");
                // move start and end
                start = end + 1;
                end = start + 16;
                line += 1;
            }
            return sb.ToString();
        }
    }
}
