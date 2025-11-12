using System.Collections.Concurrent;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json.Linq;
using Cea.Utils;
using System.IO;
using Cea.Utils.Extension;

namespace CeaToolsRunner.Reptile;

public class Reptile_Lanzou
{
    private readonly HttpClient _client;
    private readonly string _vei;
    private readonly string _uid;

    public Reptile_Lanzou(string cookie, string vei, string uid)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer()
        };
        handler.CookieContainer.AddCookieFromString(cookie, "https://up.woozooo.com");
        _client = new(handler);
        //_client.DefaultRequestHeaders.Add("Referer", $"https://up.woozooo.com/mydisk.php?item=files&action=index&u={uid}");

        _vei = vei;
        _uid = uid;
    }

    /// <summary>
    /// 获取文件列表
    /// </summary>
    /// <param name="folder_id">如果是根目录，则id为-1</param>
    /// <param name="pg"></param>
    /// <param name="vei"></param>
    /// <returns></returns>
    public async Task<List<LanzouFileWrapper>> GetFiles(string folder_id, int pg)
    {
        var response = await _client.PostWithForm($@"https://up.woozooo.com/doupload.php?uid={_uid}", new
        {
            task = 5,
            folder_id,
            pg,
            vei = _vei,
        });
        var resobj = JObject.Parse(response);
        try
        {
            return (resobj["text"] as JArray).Cast<JObject>()
                                  .Select(x => new LanzouFileWrapper($"{x["name_all"]}", $"{x["id"]}"))
                                  .ToList();
        }
        catch
        {
            return new List<LanzouFileWrapper>();
        }
    }


    public async Task<List<LanzouFileWrapper>> GetAllFilesWithUrl(string folder_id)
    {
        //getfiles.count == 0, 就需要停止了
        //这里单线程就行了，获取链接的地方搞多线程


        var all_files = new List<LanzouFileWrapper>();
        var dict = new ConcurrentDictionary<string, LanzouFileWrapper>();

        //1. 获取所有文件，这样也可以有一个进度条
        DateTime start_time = DateTime.Now;

        for (int i = 1; ; i++)
        {
            var res = await GetFiles(folder_id, i);
            if (res.Count() == 0) break;

            all_files.AddRange(res);
            await Console.Out.WriteLineAsync($"获取文件中，当前数量 {all_files.Count()}");
        }

        await Console.Out.WriteLineAsync($"文件获取完成,耗时 {DateTime.Now.Subtract(start_time).TotalSeconds:0.00} 秒");
        start_time = DateTime.Now;
        await Console.Out.WriteLineAsync($"总文件数量:{all_files.Count()}, 开始获取链接");


        SemaphoreSlim semaphore = new(32); // 设置最大并发数

        var url_tasks = new List<Task>();
        foreach (var file in all_files)
        {
            Task task = Task.Run(async () =>
            {
                await semaphore.WaitAsync(); // 获取信号量
                try
                {
                    file.url = await GetFileSharedUrl(file.id);
                    dict[file.id] = file;
                    await Console.Out.WriteLineAsync($"进度:{dict.Count() / (double)all_files.Count() * 100:0.00}%, {file}");
                }
                finally
                {
                    semaphore.Release();
                }
            });
            url_tasks.Add(task);
        }

        await Task.WhenAll(url_tasks);

        await Console.Out.WriteLineAsync($"链接获取完成,耗时 {DateTime.Now.Subtract(start_time).TotalSeconds:0.00} 秒");

        return dict.Values.OrderBy(f => f.name).ToList();
    }

    public async Task<string> GetFileSharedUrl(string file_id)
    {
        var res = await _client.PostWithForm(@"https://up.woozooo.com/doupload.php", new
        {
            task = 22,
            file_id
        });
        var obj = JObject.Parse(res)["info"];
        if (obj == null) return "";
        return $"{obj["is_newd"]}/{obj["f_id"]}";
    }


    private static readonly string _filename = Path.Combine(DataHelper.GetDataFolder("Tools1"), "lanzou.json");

    public static void SaveData(List<LanzouFileWrapper> files)
    {
        File.WriteAllText(_filename, files.ToJson());
    }

    public static IEnumerable<string> Search(List<string> key_words)
    {

        var dir = DataHelper.GetDataFolder("Tools1");
        if (!File.Exists(_filename))
        {
            throw new Exception("请先获取数据然后再进行搜索操作");
        }

        var list = File.ReadAllText(_filename).TryJsonToObject<List<LanzouFileWrapper>>();

        if (key_words.JoinToString("").Trim() == "")
        {
            foreach (var item in list)
            {
                yield return item.ToString();
            }
        }

        foreach (var key in key_words)
        {
            var search_res = list.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f.name).Sim(key.Trim()) > 0.95);
            yield return search_res?.ToString() ?? key.Trim();
        }
    }

    public static async Task Test()
    {
        var cookie = @"PHPSESSID=m407cvl638do1779fli6pk36ph7iji2o; _uab_collina=169365914974193733907643; uag=a25cb624d5b854d56ffe0543672564c9; __51cke__=; phpdisk_info=WW8DOA1tBjsCNgZlWTFbCAVhBQ4OZgBiVGcCZgc1BTRXZFFrA2hVYFdiAVgIZABqBW1RZ1o0UjNQYgQ2AGUCN1lkAzUNaQY4AjQGYFlmWzMFbAU0DmAAZ1QyAmEHZwVjV2NRZwNgVT9XZQEwCFsAawVgUWNaN1IxUGAEYwA%2BAjlZZAM2; ylogin=2921632; __tins__21412745=%7B%22sid%22%3A%201693721572088%2C%20%22vd%22%3A%201%2C%20%22expires%22%3A%201693723372088%7D; __51laig__=30; folder_id_c=7355434";
        var vei = "AlUEVFJWBgoEBldWWlI=";
        var uid = "2921632";
        var reptile = new Reptile_Lanzou(cookie, vei, uid);
        var folder_id = "7355434";
        //var res = await reptile.GetAllFilesWithUrl("7355434");

        var res = await reptile.GetFiles(folder_id, 300);
        foreach (var item in res)
        {
            await Console.Out.WriteLineAsync(item.ToString());
        }

        Console.WriteLine("done!");
    }
}

public class LanzouFileWrapper
{
    public LanzouFileWrapper(string name, string id)
    {
        this.name = name;
        this.id = id;
    }
    public string name { get; set; }
    public string id { get; set; }
    public string url { get; set; }
    public override string ToString()
    {
        return $"{name}\t{url}";
    }
}
