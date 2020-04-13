using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace LiveChatLib.Helpers
{

    public static class DeflateHelper
    {
        public static byte[] Extract(Stream inputStream)
        {
            long origPos = 0;

            if (inputStream.CanSeek)
            {
                inputStream.Seek(0, SeekOrigin.Begin);
                origPos = inputStream.Position;
            }

            var result = ExtractFromStream(inputStream, true);

            if (inputStream.CanSeek)
            {
                inputStream.Seek(origPos, SeekOrigin.Begin);
            }
            return result;
        }

        public static byte[] Extract(byte[] input)
        {
            var ms = new MemoryStream(input);
            return ExtractFromStream(ms, false);
        }

        private static byte[] ExtractFromStream(Stream inputStream, bool keepOpen)
        {
            byte[] result;

            using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress, keepOpen))
            {
                var ms = new MemoryStream();
                while (true)
                {
                    var b = deflateStream.ReadByte();
                    if (b == -1)
                    {
                        break;
                    }
                    ms.WriteByte((byte)b);
                }
                ms.Flush();
                result = ms.ToArray();
                ms.Dispose();
            }
            return result;
        }
    }
}
