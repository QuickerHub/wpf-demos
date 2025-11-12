using System.Net;
using System.Net.Http;
using Cea.Utils.Extension;
using Cea.Utils;
using System.IO;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using CeaToolsRunner.Verb;
using System.Web;

namespace CeaToolsRunner.Reptile
{
    public class Reptile_lcldsss
    {
        private readonly HttpClient _client;
        private readonly string _boundary;

        public Reptile_lcldsss(string cookie, string boundary)
        {
            this._boundary = boundary;
            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer()
            };
            handler.CookieContainer.AddCookieFromString(cookie, "https://www.lcldsss.com");
            _client = new(handler);

            _client.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
        }

        /// <summary>
        /// 使用文本搜索出一个完全匹配标题的结果
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<lcldsss_obj> Search(string text)
        {
            var url = $"https://www.lcldsss.com/admin/material/index.html?page=1&limit=15&ids=&title={text.UrlEncode()}&cate=&charge=&download=&software=&install=&tag=&user=&create_time=";
            //var request = new HttpRequestMessage(HttpMethod.Get, url);

            //request.Headers.Add("path", "/admin/material/index.html?page=1&limit=15&ids=&title=University+Lecture+Hall&cate=&charge=&download=&software=&install=&tag=&user=&create_time=");
            //var msg = await _client.SendAsync(request);

            //var res = await msg.Content.ReadAsStringAsync();
            var res = await _client.GetContent(url);
            var obj = res.TryJsonToObject<lcldsss_search_res>();
            return obj.data.FirstOrDefault(x => x.title == text)
                ?? throw new Exception("no search result, title: " + text);
        }

        public async Task<lcldsss_uploadimage_res> UploadImage(string filepath)
        {
            var formData = new MultipartFormDataContent(_boundary);

            using var fileStream = formData.AddFile(filepath);

            var url = "https://www.lcldsss.com/admin/attachment/upload.html";
            var res = await _client.PostWithMultipartForm(url, formData);
            return res.TryJsonToObject<lcldsss_uploadimage_res>();
        }

        public async Task<lcldsss_submit_res> SubmitEdit(lcldsss_obj obj, string thumb_id)
        {
            obj.cate = await GetCateNumber(obj.cate, obj.id); //需要转int
            obj.is_free = 1; //收费
            obj.charge = "5"; //价格5
            obj.thumb = thumb_id; //设置封面
            var res = await _client.PostWithForm("https://www.lcldsss.com/admin/material/publish.html", obj);
            return res.TryJsonToObject<lcldsss_submit_res>();
        }

        private static readonly Dictionary<string, string> _cate_dict_cache = new();

        public async Task<string> GetCateNumber(string cate, int id)
        {
            if (!_cate_dict_cache.ContainsKey(cate))
            {
                var res = await _client.GetContent($"https://www.lcldsss.com/admin/material/publish?id={id}");
                var html = TextUtil.GetHtmlDocNode(res.TryJsonToObject<string>());
                var options = html.SelectNodes("""//select[@name="cate"]/option""");
                foreach (var option in options)
                {
                    var cate_num = option.Attributes["value"].Value;
                    if (!string.IsNullOrEmpty(cate_num))
                    {
                        var cate_name = Regex.Replace(option.InnerText, @"&emsp;|└|\s", "");
                        _cate_dict_cache[cate_name] = cate_num;
                    }
                }
            }

            return _cate_dict_cache[cate];
        }

        public async Task<string> OneJob(string text, string filepath)
        {
            var obj = await Search(text);
            var image_id = (await UploadImage(filepath)).data.id;
            var submit_res = await SubmitEdit(obj, image_id);
            return submit_res.msg;
        }

        public async Task<List<string>> MultiJob(IList<string> lines, string dir)
        {
            ConcurrentBag<string> failed_jobs = new();

            //SemaphoreSlim semaphore = new(1); // 设置最大并发数

            //var tasks = new List<Task>();

            int count = 0;
            foreach (var line in lines)
            {
                count++;
                var count1 = count;

                Task task = Task.Run(async () =>
                {
                    //await semaphore.WaitAsync();

                    var prefix = $"{count1 / (double)lines.Count * 100:0.00}% Job {count1}/{lines.Count}";
                    try
                    {
                        var sp_lines = line.Split(new[] { '\t' }, 2);
                        var key = sp_lines[0];
                        await Console.Out.WriteLineAsync($"{prefix} start,search text: {key}");
                        var filename = sp_lines[1];
                        var filepath = Path.Combine(dir, filename);
                        var msg = await OneJob(key, filepath);
                        await Console.Out.WriteLineAsync($"{prefix} success: {msg}");
                    }
                    catch (Exception ex)
                    {
                        failed_jobs.Add(line);
                        var info = $"{prefix} failed with error: {ex.Message}; job: {line}";
                        await Console.Out.WriteLineAsync(info);
                    }

                    //finally
                    //{
                    //    semaphore.Release();
                    //}
                });

                await task;

                //tasks.Add(task);
            }

            var time_start = DateTime.Now;

            //await Task.WhenAll(tasks);

            if (failed_jobs.Count > 0)
            {
                await Console.Out.WriteLineAsync($"total {failed_jobs.Count}/{lines.Count} jobs failed");
            }

            await Console.Out.WriteLineAsync($"done. use time: {DateTime.Now.Subtract(time_start).TotalSeconds}s");
            return failed_jobs.ToList();
        }

        public static async Task Test()
        {
            //cookie 需要实时获取，不然就会失效
            var cookie = "PHPSESSID=i88cc8h84fi37qau5b7qt292jk; usermember=admin";
            var boundary = "----WebKitFormBoundaryaEB6Wtz7vU7XO2w2";

            async Task test_main()
            {
                var jobs = """
                Tyrannosaurus Rex 3 Expansion	gWulgGl.jpg
                DMs WHITE Spring - Collection 3	Lbi2Bqp.jpg
                Clint and Kath HD for Genesis 8	oNqE9sy.jpg
                University Lecture Hall	v0TNK7o.jpg
                Carra for Genesis 8 Female	VW4hOvE.jpg
                Wet Look Stockings for Genesis 8 Females	xHp56qp.jpg
                Clara for V7	z83lo05.jpg
                dForce HnC Denim Mini Dress Outfit for Genesis 8 Females	210922053721X6O8r.jpg
                JMR dForce Jackie Dress for G8F	210922054551mXJJL.jpg
                Mother Miranda for Genesis 8 and 8.1 Female	210922055544NcuCl.png
                dForce BatWing Style Outfit For Genesis 8 and 8.1 Females	210922055913nsEQt.jpg
                Yoga Club Environment Bundle	2109220602114QBfS.jpg
                """;

                var verb = new LcldsssVerb()
                {
                    Cookie = cookie,
                    JobFile = Path.Combine(Path.GetTempPath(), "PZarL1KB1aia.txt"),
                    Dir = @"D:\桌面\测试\lcldsss\调试图片文件夹",
                };

                await verb.RunOptions();
            }

            await test_main();

            //var reptile = new Reptile_lcldsss(cookie, boundary);
            //var image_res = await reptile.UploadImage(@"D:\桌面\测试\lcldsss\xHp56qp.jpg");
            //await Console.Out.WriteLineAsync(image_res.ToJson(true));

            //var msg = await reptile.OneJob("University Lecture Hall", @"D:\桌面\测试\lcldsss\v0TNK7o.jpg");
            //await Console.Out.WriteLineAsync(msg);

            //var obj = await reptile.Search("University Lecture Hall");
            //await Console.Out.WriteLineAsync(obj.ToJson(true));
            //var res = await reptile.SubmitEdit(obj, "255");
            //await Console.Out.WriteLineAsync(res.msg);
            //await Console.Out.WriteLineAsync(obj.ToJson(true));

            //var num = await reptile.GetCateNumber("场景", "108613");
            //await Console.Out.WriteLineAsync(num.ToString());
        }
    }
#pragma warning disable IDE1006 // 命名样式
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public class lcldsss_obj
    {
        public string title { get; set; }
        public string cate { get; set; }
        public int? is_free { get; set; }
        public string charge { get; set; }
        public string download { get; set; }
        public string software { get; set; }
        public string install { get; set; }
        public string tag { get; set; }
        public string content { get; set; }

        /// <summary>
        /// 图片的 id
        /// <seealso cref="lcldsss_uploadimage_res.res_data.id"/>
        /// </summary>
        public string thumb { get; set; }
        public int id { get; set; }
    }

    public class lcldsss_search_res
    {
        public int code { get; set; }
        public int count { get; set; }
        public List<lcldsss_obj> data { get; set; }
    }

    public class lcldsss_submit_res
    {
        public int code { get; set; }
        public string msg { get; set; }
        public string data { get; set; }
        public string url { get; set; }
        public int wait { get; set; }
    }

    public class lcldsss_uploadimage_res
    {
        public int code { get; set; }
        public string msg { get; set; }
        public res_data data { get; set; }
        public string url { get; set; }
        public int wait { get; set; }
        public class res_data
        {
            public string id { get; set; }
            public string src { get; set; }
        }
    }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
#pragma warning restore IDE1006 // 命名样式
}
