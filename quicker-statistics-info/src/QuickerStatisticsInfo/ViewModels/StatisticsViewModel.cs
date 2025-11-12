using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickerStatisticsInfo;

namespace QuickerStatisticsInfo.ViewModels
{
    /// <summary>
    /// ViewModel for statistics window
    /// </summary>
    public partial class StatisticsViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial string UserInfo { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string StatusText { get; set; } = "正在加载统计数据...";

        [ObservableProperty]
        public partial int TotalLikes { get; set; } = 0;

        [ObservableProperty]
        public partial int TotalActions { get; set; } = 0;

        [ObservableProperty]
        private System.Windows.Media.Brush _statusForeground = System.Windows.Media.Brushes.Gray;

        public ObservableCollection<StatisticsItemViewModel> PageStatistics { get; } = new ObservableCollection<StatisticsItemViewModel>();

        private int _cumulativeLikes = 0;

        /// <summary>
        /// Extract user ID and username from user path
        /// </summary>
        private (string userId, string username) ExtractUserInfo(string userPath)
        {
            if (string.IsNullOrEmpty(userPath))
                return (string.Empty, string.Empty);

            int hyphenIndex = userPath.IndexOf('-');
            if (hyphenIndex > 0 && hyphenIndex < userPath.Length - 1)
            {
                string userId = userPath.Substring(0, hyphenIndex);
                string username = userPath.Substring(hyphenIndex + 1);
                return (userId, username);
            }

            return (string.Empty, userPath);
        }

        /// <summary>
        /// Initialize with user path
        /// </summary>
        public void Initialize(string userPath)
        {
            // Extract user ID from user path
            var (userId, _) = ExtractUserInfo(userPath);
            
            // Username will be extracted from user page asynchronously
            if (string.IsNullOrEmpty(userId))
            {
                UserInfo = $"用户名：加载中...";
            }
            else
            {
                UserInfo = $"用户ID：{userId} 用户名：加载中...";
            }
            StatusText = "正在加载统计数据...";
            StatusForeground = System.Windows.Media.Brushes.Gray;
            PageStatistics.Clear();
            _cumulativeLikes = 0;
            TotalLikes = 0;
            TotalActions = 0;
        }

        /// <summary>
        /// Update user info from user page (call this after Initialize)
        /// </summary>
        public async Task<UserInfo> UpdateUserInfoFromUserPageAsync(string userPath)
        {
            try
            {
                using (var collector = new QuickerStatisticsCollector())
                {
                    var userInfo = await collector.ExtractUserInfoFromUserPageAsync(userPath);
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (string.IsNullOrEmpty(userInfo.UserId))
                        {
                            UserInfo = $"用户名：{userInfo.Username}";
                        }
                        else
                        {
                            UserInfo = $"用户ID：{userInfo.UserId} 用户名：{userInfo.Username}";
                        }
                    });

                    return userInfo;
                }
            }
            catch
            {
                // If extraction fails, fall back to URL parsing (with URL decode)
                var (userId, usernameFromUrl) = ExtractUserInfo(userPath);
                try
                {
                    // Try to decode URL-encoded username
                    string decodedUsername = WebUtility.UrlDecode(usernameFromUrl);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (string.IsNullOrEmpty(userId))
                        {
                            UserInfo = $"用户名：{decodedUsername}";
                        }
                        else
                        {
                            UserInfo = $"用户ID：{userId} 用户名：{decodedUsername}";
                        }
                    });

                    return new UserInfo
                    {
                        UserId = userId,
                        Username = decodedUsername,
                        UserPath = userPath
                    };
                }
                catch
                {
                    // If decode fails, use original
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (string.IsNullOrEmpty(userId))
                        {
                            UserInfo = $"用户名：{usernameFromUrl}";
                        }
                        else
                        {
                            UserInfo = $"用户ID：{userId} 用户名：{usernameFromUrl}";
                        }
                    });

                    return new UserInfo
                    {
                        UserId = userId,
                        Username = usernameFromUrl,
                        UserPath = userPath
                    };
                }
            }
        }

        /// <summary>
        /// Add page statistics (can be called from any thread)
        /// </summary>
        public void AddPageStatistics(PageStatistics pageStat, int totalLikes)
        {
            _cumulativeLikes += pageStat.LikesCount;
            
            // Update on UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                PageStatistics.Add(new StatisticsItemViewModel
                {
                    PageNumber = pageStat.PageNumber,
                    LikesCount = pageStat.LikesCount,
                    CumulativeLikes = _cumulativeLikes,
                    ActionsCount = pageStat.ActionsCount
                });
                TotalLikes = totalLikes;
                // Update TotalActions by summing all page actions
                TotalActions = PageStatistics.Sum(p => p.ActionsCount);
                StatusText = $"已加载 {PageStatistics.Count} 页数据...";
            });
        }

        /// <summary>
        /// Mark statistics collection as completed
        /// </summary>
        public void MarkCompleted()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = $"统计完成，共 {PageStatistics.Count} 页";
                StatusForeground = System.Windows.Media.Brushes.Green;
            });
        }

        /// <summary>
        /// Start collecting statistics asynchronously
        /// </summary>
        /// <param name="userPath">User path</param>
        public void StartCollecting(string userPath)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Small delay to ensure window is fully visible
                    await Task.Delay(50);

                    // Start collecting statistics (user info will be extracted from first page HTML)
                    var collector = new QuickerStatisticsCollector();
                    await collector.GetStatisticsWithProgressAsync(
                        userPath,
                        (pageStat, totalLikes) =>
                        {
                            // Update ViewModel (thread-safe)
                            AddPageStatistics(pageStat, totalLikes);
                        },
                        (userInfo) =>
                        {
                            // Update user info from first page HTML (no extra request)
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (string.IsNullOrEmpty(userInfo.UserId))
                                {
                                    UserInfo = $"用户名：{userInfo.Username}";
                                }
                                else
                                {
                                    UserInfo = $"用户ID：{userInfo.UserId} 用户名：{userInfo.Username}";
                                }
                            });
                        });
                    collector.Dispose();

                    // Mark as completed
                    MarkCompleted();
                }
                catch (Exception ex)
                {
                    MarkCompleted();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"获取统计信息失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        /// <summary>
        /// Load statistics data (for backward compatibility)
        /// </summary>
        /// <param name="result">Statistics result</param>
        public void LoadStatistics(StatisticsResult result)
        {
            if (result == null)
                return;

            Initialize(result.UserPath);

            // Add all items in order
            int cumulativeLikes = 0;
            foreach (var pageStat in result.PageStatistics.OrderBy(p => p.PageNumber))
            {
                cumulativeLikes += pageStat.LikesCount;
                AddPageStatistics(pageStat, cumulativeLikes);
            }

            // Set final totals
            Application.Current.Dispatcher.Invoke(() =>
            {
                TotalLikes = result.TotalLikes;
                TotalActions = result.TotalActions;
            });

            MarkCompleted();
        }
    }

    /// <summary>
    /// ViewModel for statistics item
    /// </summary>
    public partial class StatisticsItemViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial int PageNumber { get; set; }

        [ObservableProperty]
        public partial int LikesCount { get; set; }

        [ObservableProperty]
        public partial int CumulativeLikes { get; set; }

        [ObservableProperty]
        public partial int ActionsCount { get; set; }
    }
}

