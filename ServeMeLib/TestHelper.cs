using System.IO;
using System.Net;
using System.Text;

namespace ServeMeLib
{
    public static class TestHelper
    {
        public static HttpWebResponse HttpGet(this string url, int timeoutMilliSeconds = 300000)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Timeout = timeoutMilliSeconds;
            return (HttpWebResponse)request.GetResponse();
        }

        public static string ReadStringFromResponse(this HttpWebResponse response)
        {
            using (Stream stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static HttpWebResponse HttpPost(this string uri, string data = "", string method = "POST")
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentLength = dataBytes.Length;
            request.ContentType = "application/json";
            request.Method = method;
            using (Stream requestBody = request.GetRequestStream())
            {
                requestBody.Write(dataBytes, 0, dataBytes.Length);
            }

            return (HttpWebResponse)request.GetResponse();
        }
    }
}