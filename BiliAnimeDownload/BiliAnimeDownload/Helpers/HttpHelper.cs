using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;

namespace BiliAnimeDownload.Helpers
{
    public class HttpHelper
    {
        public static async Task<string> GetStringAsync(string url, IDictionary<string, string> headers = null)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                if (headers != null)
                {
                    foreach (var item in headers)
                    {
                        httpClient.DefaultRequestHeaders.Add(item.Key, item.Value);
                    }
                }
                var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
                response.EnsureSuccessStatusCode();
                var results = await response.Content.ReadAsStringAsync();
                return results;
            }
        }
        public static async Task<Stream> GetStreamAsync(string url, IDictionary<string, string> headers = null)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                if (headers != null)
                {
                    foreach (var item in headers)
                    {
                        httpClient.DefaultRequestHeaders.Add(item.Key, item.Value);
                    }
                }
                var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
                response.EnsureSuccessStatusCode();
                var results = await response.Content.ReadAsStreamAsync();
                return results;
            }
        }


    }
}
