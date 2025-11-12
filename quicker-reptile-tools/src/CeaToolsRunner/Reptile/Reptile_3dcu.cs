using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Cea.Utils;

namespace CeaToolsRunner.Reptile;

public class Reptile_3dcu
{
    public Reptile_3dcu(string txt)
    {
        this._txt = txt;
        this._dir = Path.GetDirectoryName(txt);
    }

    private static readonly HttpClient _client = new();
    private readonly string _txt;
    private readonly string _dir;

    public async Task<(string img, string href)> Search(string text)
    {
        var url = @"https://3dcu.com/index.php?do=search";

        var html = await _client.PostWithForm(url, new
        {
            @do = "search",
            subaction = "search",
            search_start = 0,
            full_search = 0,
            result_from = 1,
            story = text.Trim(),
            titleonly = 3
        });

        var root = TextUtil.GetHtmlDocNode(html);
        var boxes_list = root.SelectNodes(@"//div[@class='boxes']");
        if (boxes_list != null)
        {
            foreach (var box in boxes_list)
            {
                var img = box.SelectSingleNode(@"div[@class='boxes-image']/a/img").Attributes["src"].Value;
                var a = box.SelectSingleNode(@"div[@class='boxes-content']/a");
                var href = a.Attributes["href"].Value;
                var title = a.InnerText;
                if (title == text)
                {
                    return (img, href);
                }
            }
        }

        throw new Exception("no search result");
    }

    public async Task<string> Flow(string text)
    {
        try
        {
            (string img, string href) = await Search(text);
            var root = TextUtil.GetHtmlDocNode(await _client.GetContent(href));
            var a = root.SelectSingleNode("""//*[@class="news-content"]/a""");
            if (a != null)
            {
                return $"{img}\t{a.Attributes["href"].Value}";
            }
            var url_text = root.SelectSingleNode("""//*[@class="news-content"]""").InnerText;
            var url = TextUtil.ExtractFirstUrl(url_text);
            return $"{img}\t{url}";
        }
        catch (Exception e)
        {
            await Console.Out.WriteLineAsync(e.Message);
            return "";
        }
    }

    public List<string> GetSearchTextList()
    {
        return File.ReadAllText(_txt).Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    public async Task Run()
    {
        var list = GetSearchTextList();

        var dict = new ConcurrentDictionary<string, string>();
        async Task Run(string text)
        {
            var idx = list.IndexOf(text);
            await Console.Out.WriteLineAsync($"Task {idx} start: {text}");
            var res = await Flow(text);
            if (res == "")
            {
                await Console.Out.WriteLineAsync($"Task {idx} failed");
            }
            else
            {
                await Console.Out.WriteLineAsync($"Task {idx} compite: {res}");
            }
            dict[text] = res;
        }

        var tasks = new List<Task>();
        SemaphoreSlim semaphore = new(5); // 设置最大并发数为5

        foreach (string item in list)
        {
            Task task = Task.Run(async () =>
            {
                await semaphore.WaitAsync(); // 获取信号量
                try
                {
                    await Run(item);
                }
                finally
                {
                    semaphore.Release(); // 释放信号量
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        //var file = Path.Combine(_dir,"爬取图片.json");

        //File.WriteAllText(file, dict.ToJson(true));
        var file = Path.Combine(_dir, Guid.NewGuid() + ".txt");

        File.WriteAllText(file, string.Join("\r\n", list.Select(x => x + "\t" + dict[x])));

        new Process() { StartInfo = new() { FileName = file } }.Start();
    }

    public static async Task CMD()
    {
        string txt = "";
        while (!File.Exists(txt))
        {
            Console.WriteLine("请输入txt文件路径，txt文件中每一行一个名称");
            txt = Console.ReadLine();
        }

        var rep = new Reptile_3dcu(txt);

        await rep.Run();
    }
}