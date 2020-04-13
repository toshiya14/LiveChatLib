using LiveChatLib.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiveChatLib.Bilibili
{
    public static class PackageParser
    {
        /// <summary>
        /// Parse package from byte array to Package Object.
        /// </summary>
        /// <param name="buffer">package binary data.</param>
        /// <returns></returns>
        public static IEnumerable<Package> GetPackages(byte[] buffer)
        {
            var part = SplitBuffer(buffer);
            var list = new List<Package>();
            foreach (var p in part)
            {
                var package = Package.LoadFromByteArray(p);
                list.AddRange(package);
            }
            return list;
        }

        /// <summary>
        /// Split binary frame might contains multiple packages into package binary data enumerable.
        /// </summary>
        /// <param name="buffer">package binary data.</param>
        /// <returns></returns>
        private static IEnumerable<byte[]> SplitBuffer(byte[] buffer)
        {
            using (var ms = new MemoryStream(buffer))
            {
                ms.Seek(0, SeekOrigin.Begin);
                using (var reader = new BinaryReader(ms))
                {
                    while (ms.Position < ms.Length)
                    {
                        var length = reader.ReadBytes(4).ByteToInt32(true);
                        if (length > buffer.Length)
                        {
                            throw new Exception("Binary frame broken, package size:" + buffer.Length + ", size in header:" + length);
                        }
                        ms.Seek(-4, SeekOrigin.Current);
                        var pack = reader.ReadBytes(length);
                        yield return pack;
                    }
                }
            }
        }
    }
}
