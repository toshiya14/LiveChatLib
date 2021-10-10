using LiveChatLib.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            IEnumerable<byte[]> parts = null;
            var list = new List<Package>();
            try
            {
                parts = SplitBuffer(buffer);

                foreach (var p in parts)
                {
                    var package = Package.LoadFromByteArray(p);
                    list.AddRange(package);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception happened during SplitBuffer: {ex.Message}.");
                Trace.TraceError("\r\n" + buffer.DisplayBytes());
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
                        byte[] pack = null;
                        // Check the remaining length is greater than 4
                        if (ms.Length - ms.Position < 4)
                        {
                            throw new Exception("Binary frame broken, the package size smaller than 4 bytes, could read meta data.");
                        }
                        var length = reader.ReadBytes(4).ByteToInt32(true);
                        if (length > buffer.Length)
                        {
                            throw new Exception("Binary frame broken, package size:" + buffer.Length + ", size in header:" + length);
                        }
                        ms.Seek(-4, SeekOrigin.Current);
                        pack = reader.ReadBytes(length);
                        if (pack != null)
                        {
                            yield return pack;
                        }
                    }
                }
            }
        }
    }
}
