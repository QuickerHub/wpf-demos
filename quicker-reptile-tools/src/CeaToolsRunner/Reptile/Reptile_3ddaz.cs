using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.Net.Http;
using Cea.Utils;
using Cea.Utils.Extension;

namespace CeaToolsRunner.Reptile;

public class Reptile_3ddaz 
{
    private static readonly HttpClient client = new();

    public async Task<Dictionary<string, string>> GetCategoriesAsync()
    {
        var html = await client.GetContent(@"https://www.3ddaz.com/category/body");
        var root = TextUtil.GetHtmlDocNode(html);

        var cate_list = root.SelectSingleNode("""//*[@id="main"]/div[1]/div/div/ul[1]""")
                            .SelectNodes(@"li/a");
        return cate_list.ToDictionary(a => a.InnerText.Trim(), a => a.Attributes["href"].Value);
        //*[@id="main"]/div[1]/div/div/ul[1]/li[2]/a
    }

    public async Task<Dictionary<string, string>> GetTagsAsync(string url, bool use2 = false)
    {
        var html = await client.GetContent(url);
        var root = TextUtil.GetHtmlDocNode(html);

        HtmlAgilityPack.HtmlNode? tags_root = null;

        if (!use2) tags_root = root.SelectSingleNode("""//*[@id="main"]/div[1]/div/div/ul[3]""");
        tags_root ??= root.SelectSingleNode("""//*[@id="main"]/div[1]/div/div/ul[2]""");

        var tags = tags_root.SelectNodes(@"li/a");
        return tags.ToDictionary(a => a.InnerText.Trim(), a =>
        {
            var href = a.Attributes["href"].Value;
            if (href.StartsWith("?"))
                return url + href;
            return href;
        });
    }

    public async Task<List<string>> GetImagePages(string url)
    {
        var root = TextUtil.GetHtmlDocNode(await client.GetContent(url));

        var span = root.SelectSingleNode("""//*[@id="main"]/div[2]/div/div/div/div[2]/span""")?.InnerText;
        if (span == null)
            return new List<string>() { url };
        var pagecount = Convert.ToInt32(span.Split('/')[1]);
        var urls = new List<string>();
        for (var i = 1; i <= pagecount; i++)
        {
            if (url.Contains("?tag="))
            {
                var idx = url.IndexOf("?");
                urls.Add(url.Substring(0, idx) + $"/page/{i}" + url.Substring(idx));
            }
            else
            {
                urls.Add(url + $"/page/{i}");
            }
        }
        return urls;
    }

    public async Task<List<string>> GetArticles(string url, string tag)
    {
        var root = TextUtil.GetHtmlDocNode(await client.GetContent(url));
        var articles = root.SelectNodes(@"//article");
        var list = new List<string>();
        var taglist = new List<string>() { tag };
        foreach (var article in articles)
        {
            var href = article.SelectSingleNode(@"div/div/a").Attributes["href"].Value;
            taglist.AddRange(article.SelectNodes(@"div/span/a").Select(x => x.InnerText));
            var tags = taglist.Distinct().JoinToString(",");
            list.Add(href + "\t" + tags);
        }
        return list;
    }

    static async Task RunAsync()
    {
        var rep = new Reptile_3ddaz();

        //var res = await rep.GetArticles(@"https://www.3ddaz.com/category/hair?tag=g2m", "G2男");
        //Console.WriteLine(res.JoinToString());

        var categories = await rep.GetCategoriesAsync();
        categories.Remove("推荐");
        categories.Remove("亚洲");

        var files = Directory.GetFiles(@"D:\work\JavaScript\定制动作\3ddaz");
        foreach (var file in files)
        {
            categories.Remove(Path.GetFileName(file));
        }

        var remove_tag = new[] { "G2", "G3", "G8.0", "G8.1", "IM", "女性", "男性" };

        var table = new DataTable();

        foreach (var category in categories)
        {
            Console.WriteLine($"{category.Key}:{category.Value}");

            var tags = await rep.GetTagsAsync(category.Value, category.Key.EqualsAny(false, "灯光材质渲染", "生物机械", "武器道具"));
            foreach (var tag in remove_tag)
            {
                tags.Remove(tag);
            }

            Console.WriteLine(tags.ToJson(true));

            var list = new List<string>();

            foreach (var tag in tags)
            {
                var pages = await rep.GetImagePages(tag.Value);
                Console.WriteLine($"tag:{tag.Key} pages:{pages.Count}");
                var temp_dict = new ConcurrentDictionary<string, List<string>>();

                var tasks = new List<Task>();
                SemaphoreSlim semaphore = new(10);

                foreach (var page in pages)
                {
                    Task task = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            temp_dict[page] = await rep.GetArticles(page, tag.Key);
                        }
                        finally
                        {
                            semaphore.Release();
                            Console.WriteLine(page);
                        }
                    });
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);

                foreach (var page in pages)
                {
                    list.AddRange(temp_dict[page]);
                }
            }

            File.WriteAllText(@$"D:\work\JavaScript\定制动作\3ddaz\{category.Key}", list.JoinToString());
            //table.Columns.Add(category.Key, typeof(string));
        }

        CommonUtil.TryOpenFileOrUrl(@"D:\work\JavaScript\定制动作\3ddaz");
    }
}