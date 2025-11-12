using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Cea.Utils;
using HtmlAgilityPack;

namespace CeaToolsRunner.Reptile
{
    public class Reptile_Miyoushe
    {
        private readonly HttpClient _client;
        public Reptile_Miyoushe()
        {
            _client = new();
        }

        public static async Task Test()
        {
            string url = "https://bbs-api.miyoushe.com/post/wapi/getPostReplies?gids=2&is_hot=false&order_type=1&post_id=53602043&size=20";

            using (var handler = new HttpClientHandler())
            {
                handler.CookieContainer = new CookieContainer();

                // 添加 cookies
                handler.CookieContainer.Add(new Uri("https://bbs-api.miyoushe.com"), new Cookie("_MHYUUID", "3c6b43c4-34a1-4bf4-8052-d3054abb8a3f", "/", ".miyoushe.com"));
                handler.CookieContainer.Add(new Uri("https://bbs-api.miyoushe.com"), new Cookie("DEVICEFP_SEED_ID", "5ec7c8e7b57cd409", "/", ".miyoushe.com"));
                handler.CookieContainer.Add(new Uri("https://bbs-api.miyoushe.com"), new Cookie("DEVICEFP_SEED_TIME", "1700218756567", "/", ".miyoushe.com"));
                handler.CookieContainer.Add(new Uri("https://bbs-api.miyoushe.com"), new Cookie("_ga", "GA1.1.100115758.1700218757", "/", ".miyoushe.com"));

                handler.CookieContainer.Add(new Uri("https://bbs-api.miyoushe.com"), new Cookie("DEVICEFP", "38d7fadd369b9", "/", ".miyoushe.com"));
                handler.CookieContainer.Add(new Uri("https://bbs-api.miyoushe.com"), new Cookie("acw_tc", "0a340b6417178352175597578e78d935809cc7fffc6b5df0851dbe526d5137", "/", "bbs-api.miyoushe.com"));
                handler.CookieContainer.Add(new Uri("https://bbs-api.miyoushe.com"), new Cookie("_ga_KS4J8TXSHQ", "GS1.1.1717833748.8.1.1717835316.0.0.0", "/", ".miyoushe.com"));

                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("zstd"));
                    client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
                    client.DefaultRequestHeaders.Add("DS", "1717835317,bsiKDD,4f4ed23c710c77fd5d8c7fd718012ff8");
                    client.DefaultRequestHeaders.Add("Origin", "https://www.miyoushe.com");
                    client.DefaultRequestHeaders.Referrer = new Uri("https://www.miyoushe.com/");
                    client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
                    client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
                    client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
                    client.DefaultRequestHeaders.Add("sec-ch-ua", "\"Google Chrome\";v=\"125\", \"Chromium\";v=\"125\", \"Not.A/Brand\";v=\"24\"");
                    client.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
                    client.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
                    client.DefaultRequestHeaders.Add("x-rpc-app_version", "2.71.0");
                    client.DefaultRequestHeaders.Add("x-rpc-client_type", "4");
                    client.DefaultRequestHeaders.Add("x-rpc-device_fp", "38d7fadd369b9");
                    client.DefaultRequestHeaders.Add("x-rpc-device_id", "e610e9903c8a46ca038848556e993b3f");

                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseBody);
                }
            }
        }

        public async void Run()
        {
            var url = @"https://www.miyoushe.com/ys/article/53599167#reply";
            var text = await _client.GetContent(url);
            await Console.Out.WriteLineAsync(text);
        }

    }

}
