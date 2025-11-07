using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace StepsChanger.Services
{
    /// <summary>
    /// Steps modification service - integrates login, token management, and step submission
    /// </summary>
    public class StepsService : IDisposable
    {
        private readonly ZeppAuthService _authService;
        private readonly HttpClient _httpClient;

        public StepsService()
        {
            _authService = new ZeppAuthService();
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Get user info to verify token
        /// </summary>
        public async Task<(bool success, string? userId, string? error)> GetUserInfoAsync(string appToken)
        {
            try
            {
                var url = "https://api-mifit-cn3.zepp.com/v1/users/me.json";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                request.Headers.Add("User-Agent", "Zepp/9.13.0 (iPhone; iOS 26.1; Scale/3.00)");
                request.Headers.Add("apptoken", appToken);
                request.Headers.Add("appname", "com.huami.midong");
                request.Headers.Add("appplatform", "ios_phone");
                request.Headers.Add("lang", "zh_CN");
                request.Headers.Add("country", "CN");
                request.Headers.Add("Accept", "*/*");

                var response = await _httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    try
                    {
                        var json = JObject.Parse(responseText);
                        var userId = json["data"]?["userid"]?.ToString();
                        if (!string.IsNullOrEmpty(userId))
                        {
                            return (true, userId, null);
                        }
                    }
                    catch
                    {
                        // Parse error
                    }
                }

                return (false, null, $"获取用户信息失败: {responseText}");
            }
            catch (Exception ex)
            {
                return (false, null, $"请求异常: {ex.Message}");
            }
        }

        /// <summary>
        /// Submit steps data to Zepp Life
        /// </summary>
        /// <param name="appToken">App token from Zepp authentication</param>
        /// <param name="userId">User ID</param>
        /// <param name="steps">Number of steps to set</param>
        /// <param name="date">Date for steps (format: yyyy-MM-dd), null for today</param>
        public async Task<(bool success, string? error)> SubmitStepsAsync(
            string appToken, string userId, int steps, DateTime? date = null)
        {
            try
            {
                if (date == null)
                {
                    date = DateTime.Today;
                }

                // Format date as timestamp (seconds since epoch)
                long timestamp = ((DateTimeOffset)date.Value).ToUnixTimeSeconds();

                // Build request URL
                var url = $"https://api-mifit-cn3.zepp.com/v1/data/band_data.json";

                // Build request data
                var data = new Dictionary<string, string>
                {
                    { "userid", userId },
                    { "data_type", "2" }, // 2 = steps data
                    { "timestamp", timestamp.ToString() },
                    { "steps", steps.ToString() }
                };

                var content = new FormUrlEncodedContent(data);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                // Set headers
                request.Headers.Add("User-Agent", "Zepp/9.13.0 (iPhone; iOS 26.1; Scale/3.00)");
                request.Headers.Add("apptoken", appToken);
                request.Headers.Add("appname", "com.huami.midong");
                request.Headers.Add("appplatform", "ios_phone");
                request.Headers.Add("lang", "zh_CN");
                request.Headers.Add("country", "CN");
                request.Headers.Add("timezone", "Asia/Shanghai");
                request.Headers.Add("Accept", "*/*");
                request.Headers.Add("Accept-Language", "zh-Hans-CN;q=1, en-CN;q=0.9");
                request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                request.Headers.Add("Connection", "keep-alive");
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

                var response = await _httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    try
                    {
                        var json = JObject.Parse(responseText);
                        if (json["code"]?.ToString() == "0" || json["result"]?.ToString() == "ok")
                        {
                            return (true, null);
                        }
                        else
                        {
                            return (false, json["message"]?.ToString() ?? "未知错误");
                        }
                    }
                    catch
                    {
                        // If response is not JSON, check if it contains success indicators
                        if (responseText.Contains("success") || responseText.Contains("ok"))
                        {
                            return (true, null);
                        }
                        return (false, responseText);
                    }
                }
                else
                {
                    return (false, $"HTTP {response.StatusCode}: {responseText}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"提交步数失败: {ex.Message}");
            }
        }

        /// <summary>
        /// Main method to change steps - requires account, password, and target steps
        /// </summary>
        public async Task<(bool success, string? error)> ChangeStepsAsync(string account, string password, int steps)
        {
            try
            {
                // Step 1: Login and get access token
                var (token, error) = await _authService.GetAccessTokenAsync(account, password);
                if (string.IsNullOrEmpty(token) || !string.IsNullOrEmpty(error))
                {
                    return (false, error ?? "登录失败");
                }

                // Step 2: Get user info (userid)
                var userInfoResult = await GetUserInfoAsync(token);
                if (!userInfoResult.success || string.IsNullOrEmpty(userInfoResult.userId))
                {
                    return (false, userInfoResult.error ?? "获取用户信息失败");
                }

                // Step 3: Submit steps
                var (success, submitError) = await SubmitStepsAsync(token, userInfoResult.userId, steps);
                if (!success)
                {
                    return (false, submitError ?? "提交步数失败");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"修改步数异常: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _authService?.Dispose();
            _httpClient?.Dispose();
        }
    }
}
