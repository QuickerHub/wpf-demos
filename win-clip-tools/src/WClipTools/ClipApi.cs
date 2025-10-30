using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using wclip = Windows.ApplicationModel.DataTransfer.Clipboard;

namespace WClipTools
{
    public class ClipApi
    {
        public ClipApi()
        {
            //只能在win10上面使用
        }
        public async Task<object?> testAsync()
        {
            string op = ""; //at;take;
            var api = new ClipApi();
            var count = 1;
            return op switch
            {
                "at" => await api.GetClipTextAt(count),
                "take" => await api.GetClipTextAsync(count),
                _ => null,
            };
        }

        private async Task<IReadOnlyList<ClipboardHistoryItem>> GetItemsAsync() => (await wclip.GetHistoryItemsAsync()).Items;
        private async Task<string> GetItemText(ClipboardHistoryItem item)
        {
            try
            {
                return await item.Content.GetTextAsync();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 通过序号获取剪贴板项
        /// </summary>
        /// <param name="num">序号，从1开始</param>
        /// <returns></returns>
        public async Task<string> GetClipTextAt(int num)
        {
            var items = await GetItemsAsync();
            string text;
            if (num < 0)
            {
                return "";
            }

            foreach (var item in items)
            {
                text = await GetItemText(item);
                if (!string.IsNullOrEmpty(text))
                {
                    num--;
                    if (num == 0)
                        return text;
                }
            }
            return "";
        }

        /// <summary>
        /// 通过索引获取剪贴板项
        /// </summary>
        /// <param name="index">从 0 开始</param>
        /// <returns></returns>
        public async Task<string> GetClipTextAcIndex(int index) => await GetClipTextAt(index + 1);

        /// <summary>
        /// 获取一列剪贴板项
        /// </summary>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public async Task<List<string>> GetClipTextAsync(int count)
        {
            var res = await wclip.GetHistoryItemsAsync();
            var items = res.Items;
            string text;
            var textList = new List<string>();
            foreach (var item in items)
            {
                try
                {
                    text = await item.Content.GetTextAsync();
                }
                catch
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(text))
                {
                    textList.Add(text);
                    if (textList.Count >= count)
                        return textList;
                }
            }
            return textList;
        }

        public void Clear() => wclip.Clear();
        public bool ClearHistory() => wclip.ClearHistory();
    }
}
