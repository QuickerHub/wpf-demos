using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace QuickerStatisticsInfo
{
    /// <summary>
    /// Collector for Quicker statistics information
    /// </summary>
    public class QuickerStatisticsCollector : IDisposable
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://getquicker.net";

        public QuickerStatisticsCollector()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        /// <summary>
        /// Get total likes count from user actions page
        /// </summary>
        /// <param name="userPath">User path, e.g., "113342-Cea"</param>
        /// <returns>Total likes count</returns>
        public async Task<int> GetTotalLikesAsync(string userPath)
        {
            int totalLikes = 0;
            int currentPage = 1;
            bool hasMorePages = true;

            while (hasMorePages)
            {
                var url = $"/User/Actions/{userPath}?p={currentPage}";
                var html = await _httpClient.GetStringAsync(url);
                
                var (likes, _, maxPage) = ParsePage(html);
                totalLikes += likes;

                // Check if there are more pages
                if (currentPage >= maxPage)
                {
                    hasMorePages = false;
                }
                else
                {
                    currentPage++;
                }

                // Small delay to avoid overwhelming the server
                await Task.Delay(500);
            }

            return totalLikes;
        }

        /// <summary>
        /// Parse HTML page and extract likes count, actions count and max page number
        /// </summary>
        /// <param name="html">HTML content</param>
        /// <returns>Tuple of (likes count on this page, actions count on this page, max page number)</returns>
        private (int likes, int actionsCount, int maxPage) ParsePage(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            int pageLikes = 0;
            int actionsCount = 0;
            int maxPage = 1;

            // Find the header row to locate "获赞" column index
            var headerRow = doc.DocumentNode.SelectSingleNode("//table//tr[1]");
            int likesColumnIndex = -1;
            
            if (headerRow != null)
            {
                var headerCells = headerRow.SelectNodes(".//th | .//td");
                if (headerCells != null)
                {
                    for (int i = 0; i < headerCells.Count; i++)
                    {
                        var headerText = headerCells[i].InnerText.Trim();
                        if (headerText == "获赞")
                        {
                            likesColumnIndex = i;
                            break;
                        }
                    }
                }
            }

            // Find the table rows (skip header row)
            var rows = doc.DocumentNode.SelectNodes("//table//tr[position()>1]");
            if (rows != null)
            {
                actionsCount = rows.Count;
                
                if (likesColumnIndex >= 0)
                {
                    foreach (var row in rows)
                    {
                        var cells = row.SelectNodes(".//td");
                        if (cells != null && cells.Count > likesColumnIndex)
                        {
                            var likesCell = cells[likesColumnIndex];
                            var likesText = likesCell.InnerText.Trim();

                            // Parse likes count (handle empty or non-numeric values)
                            if (int.TryParse(likesText, out int likes))
                            {
                                pageLikes += likes;
                            }
                        }
                    }
                }
            }

            // Find max page number from pagination
            var paginationLinks = doc.DocumentNode.SelectNodes("//nav//a[contains(@href, '?p=')]");
            if (paginationLinks != null)
            {
                foreach (var link in paginationLinks)
                {
                    var href = link.GetAttributeValue("href", "");
                    var match = Regex.Match(href, @"\?p=(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int pageNum))
                    {
                        if (pageNum > maxPage)
                        {
                            maxPage = pageNum;
                        }
                    }
                }
            }

            // Also check for "末页" (last page) link
            var lastPageLink = doc.DocumentNode.SelectSingleNode("//nav//a[contains(text(), '末页')]");
            if (lastPageLink != null)
            {
                var href = lastPageLink.GetAttributeValue("href", "");
                var match = Regex.Match(href, @"\?p=(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int lastPage))
                {
                    maxPage = Math.Max(maxPage, lastPage);
                }
            }

            return (pageLikes, actionsCount, maxPage);
        }

        /// <summary>
        /// Get detailed statistics including likes per page
        /// </summary>
        /// <param name="userPath">User path, e.g., "113342-Cea"</param>
        /// <returns>Statistics result</returns>
        public async Task<StatisticsResult> GetStatisticsAsync(string userPath)
        {
            var result = new StatisticsResult
            {
                UserPath = userPath,
                PageStatistics = new List<PageStatistics>()
            };

            int currentPage = 1;
            bool hasMorePages = true;

            while (hasMorePages)
            {
                var url = $"/User/Actions/{userPath}?p={currentPage}";
                var html = await _httpClient.GetStringAsync(url);
                
                var (likes, actionsCount, maxPage) = ParsePage(html);
                
                result.PageStatistics.Add(new PageStatistics
                {
                    PageNumber = currentPage,
                    LikesCount = likes,
                    ActionsCount = actionsCount
                });

                result.TotalLikes += likes;
                result.TotalActions += actionsCount;

                // Check if there are more pages
                if (currentPage >= maxPage)
                {
                    hasMorePages = false;
                }
                else
                {
                    currentPage++;
                }

                // Small delay to avoid overwhelming the server
                await Task.Delay(500);
            }

            return result;
        }

        /// <summary>
        /// Get detailed statistics with progress callback for real-time updates
        /// </summary>
        /// <param name="userPath">User path, e.g., "113342-Cea"</param>
        /// <param name="progressCallback">Callback function called after each page is processed</param>
        /// <param name="userInfoCallback">Optional callback to receive user info extracted from first page</param>
        /// <returns>Statistics result</returns>
        public async Task<StatisticsResult> GetStatisticsWithProgressAsync(string userPath, Action<PageStatistics, int>? progressCallback = null, Action<UserInfo>? userInfoCallback = null)
        {
            var result = new StatisticsResult
            {
                UserPath = userPath,
                PageStatistics = new List<PageStatistics>()
            };

            int currentPage = 1;
            bool hasMorePages = true;

            while (hasMorePages)
            {
                // Fetch page HTML
                var url = $"/User/Actions/{userPath}?p={currentPage}";
                var html = await _httpClient.GetStringAsync(url);

                // Extract user info from first page HTML (only once)
                if (currentPage == 1 && userInfoCallback != null)
                {
                    var userInfo = ExtractUserInfoFromHtml(html, userPath);
                    userInfoCallback(userInfo);
                }

                var (likes, actionsCount, maxPage) = ParsePage(html);
                
                var pageStat = new PageStatistics
                {
                    PageNumber = currentPage,
                    LikesCount = likes,
                    ActionsCount = actionsCount
                };

                result.PageStatistics.Add(pageStat);
                result.TotalLikes += likes;
                result.TotalActions += actionsCount;

                // Call progress callback
                progressCallback?.Invoke(pageStat, result.TotalLikes);

                // Check if there are more pages
                if (currentPage >= maxPage)
                {
                    hasMorePages = false;
                }
                else
                {
                    currentPage++;
                }

                // Small delay to avoid overwhelming the server
                await Task.Delay(500);
            }

            return result;
        }

        /// <summary>
        /// Extract user info from HTML (without making a new request)
        /// </summary>
        /// <param name="html">HTML content from user page</param>
        /// <param name="userPath">User path</param>
        /// <returns>UserInfo</returns>
        private UserInfo ExtractUserInfoFromHtml(string html, string userPath)
        {
            var userInfo = new UserInfo
            {
                UserPath = userPath
            };

            try
            {
                // Extract user ID from user path
                int hyphenIndex = userPath.IndexOf('-');
                if (hyphenIndex > 0 && hyphenIndex < userPath.Length - 1)
                {
                    userInfo.UserId = userPath.Substring(0, hyphenIndex);
                }

                // Parse HTML to find username
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Try XPath: /html/body/div[1]/div/div[2]/h2/div[2]/div[1]/span
                var usernameNode = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div/div[2]/h2/div[2]/div[1]/span");
                if (usernameNode != null)
                {
                    var username = usernameNode.InnerText.Trim();
                    if (!string.IsNullOrEmpty(username))
                    {
                        userInfo.Username = username;
                        return userInfo;
                    }
                }

                // Fallback: Try to find username in page title
                var titleNode = doc.DocumentNode.SelectSingleNode("//title");
                if (titleNode != null)
                {
                    var title = titleNode.InnerText;
                    var match = Regex.Match(title, @"^([^分享]+)分享的动作");
                    if (match.Success && match.Groups.Count > 1)
                    {
                        userInfo.Username = match.Groups[1].Value.Trim();
                        return userInfo;
                    }
                }

                // Fallback: Try to find in breadcrumb navigation
                var breadcrumbLink = doc.DocumentNode.SelectSingleNode("//nav[@class='breadcrumb']//a[contains(@href, '/User/Actions/')]");
                if (breadcrumbLink != null)
                {
                    var text = breadcrumbLink.InnerText.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        userInfo.Username = text;
                        return userInfo;
                    }
                }
            }
            catch
            {
                // Return userInfo with empty username
            }

            return userInfo;
        }

        /// <summary>
        /// Extract user information from user page HTML (single request)
        /// This method is kept for backward compatibility, but it's better to use GetStatisticsWithProgressAsync with userInfoCallback
        /// </summary>
        /// <param name="userPath">User path like "113342-Cea"</param>
        /// <returns>UserInfo with userId and username</returns>
        public async Task<UserInfo> ExtractUserInfoFromUserPageAsync(string userPath)
        {
            try
            {
                // Fetch the user page HTML
                var url = $"/User/Actions/{userPath}";
                var html = await _httpClient.GetStringAsync(url);
                
                return ExtractUserInfoFromHtml(html, userPath);
            }
            catch
            {
                return new UserInfo { UserPath = userPath };
            }
        }

        /// <summary>
        /// Extract user path from action page URL
        /// </summary>
        /// <param name="actionUrl">Action page URL like "/Sharedaction?code=..."</param>
        /// <returns>User path like "113342-Cea"</returns>
        public async Task<string> ExtractUserPathFromActionPageAsync(string actionUrl)
        {
            try
            {
                // Remove base URL if present
                if (actionUrl.StartsWith("http://") || actionUrl.StartsWith("https://"))
                {
                    var uri = new Uri(actionUrl);
                    actionUrl = uri.PathAndQuery;
                }

                // Fetch the action page HTML
                var html = await _httpClient.GetStringAsync(actionUrl);
                
                // Parse HTML to find user link
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Try multiple selectors to find the user link
                // Option 1: Look for link with pattern /User/{id}/{name}
                var userLink = doc.DocumentNode.SelectSingleNode("//a[starts-with(@href, '/User/')]");
                
                if (userLink != null)
                {
                    var href = userLink.GetAttributeValue("href", "");
                    // Extract user path from /User/{id}/{name} -> {id}-{name}
                    var match = Regex.Match(href, @"/User/(\d+)/([^/]+)");
                    if (match.Success && match.Groups.Count >= 3)
                    {
                        string userId = match.Groups[1].Value;
                        string username = match.Groups[2].Value;
                        return $"{userId}-{username}";
                    }
                }

                // Option 2: Try the specific CSS selector path provided
                // body > div.body-wrapper > div.container.bg-white.pb-2.rounded-bottom > div > div.col-12.col-md-9.d-flex > div.pl-3.pt-0.flex-grow-1 > div.font14 > span:nth-child(1) > strong > a
                var specificLink = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'font14')]//span[1]//strong//a[starts-with(@href, '/User/')]");
                if (specificLink != null)
                {
                    var href = specificLink.GetAttributeValue("href", "");
                    var match = Regex.Match(href, @"/User/(\d+)/([^/]+)");
                    if (match.Success && match.Groups.Count >= 3)
                    {
                        string userId = match.Groups[1].Value;
                        string username = match.Groups[2].Value;
                        return $"{userId}-{username}";
                    }
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// User information extracted from user page
    /// </summary>
    public class UserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string UserPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Statistics result
    /// </summary>
    public class StatisticsResult
    {
        public string UserPath { get; set; } = string.Empty;
        public int TotalLikes { get; set; }
        public int TotalActions { get; set; }
        public List<PageStatistics> PageStatistics { get; set; } = new List<PageStatistics>();
    }

    /// <summary>
    /// Page statistics
    /// </summary>
    public class PageStatistics
    {
        public int PageNumber { get; set; }
        public int LikesCount { get; set; }
        public int ActionsCount { get; set; }
    }
}

