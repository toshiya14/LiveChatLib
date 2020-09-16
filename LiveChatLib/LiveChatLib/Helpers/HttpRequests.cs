using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LiveChatLib.Helpers
{
    public class HttpRequests
    {

        public static async Task<string> DownloadString(string url, Dictionary<string, string> headers = null, Encoding encoding = null)
        {
            string result;
            using (var client = new WebClient())
            {
                client.Encoding = encoding ?? Encoding.UTF8;
                if (headers != null && headers.Count > 0)
                {
                    foreach (var header in headers)
                    {
                        client.Headers.Add(header.Key, header.Value);
                    }
                }
                result = await client.DownloadStringTaskAsync(url);
            }
            return result;
        }

        public static async Task<byte[]> DownloadBytes(string url) {
            byte[] result;
            using (var client = new WebClient())
            {
                result = await client.DownloadDataTaskAsync(url);
            }
            return result;
        }

        public static async Task<string> Post(string url, Dictionary<string, string> formData, Dictionary<string, string> headers, Encoding encoding = null)
        {
            string result;
            using(var client = new WebClient())
            {
                client.Encoding = encoding ?? Encoding.UTF8;

                NameValueCollection form = new NameValueCollection();
                foreach(var kv in formData)
                {
                    form[kv.Key] = kv.Value;
                }
                foreach(var header in headers)
                {
                    client.Headers.Add(header.Key, header.Value);
                }
                var respdata = await client.UploadValuesTaskAsync(url, "POST", form);
                result = encoding.GetString(respdata);
            }

            return result;
        }
    }
}
