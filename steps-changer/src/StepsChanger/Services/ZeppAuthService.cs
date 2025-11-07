using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace StepsChanger.Services
{
    /// <summary>
    /// Zepp authentication service for login and token management
    /// </summary>
    public class ZeppAuthService : IDisposable
    {
        private static readonly byte[] AES_KEY = Encoding.UTF8.GetBytes("xeNtBVqzDc6tuNTh");
        private static readonly byte[] AES_IV = Encoding.UTF8.GetBytes("MAAAYAAAAAAAAABg");
        private static readonly string LOGIN_URL = "https://api-user.zepp.com/v2/registrations/tokens";
        
        private readonly HttpClient _httpClient;

        public ZeppAuthService()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Encrypt data using AES-128-CBC
        /// </summary>
        private byte[] EncryptData(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = AES_KEY;
                aes.IV = AES_IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                
                // Calculate padding length
                int padLen = 16 - (plainBytes.Length % 16);
                byte[] padded = new byte[plainBytes.Length + padLen];
                Array.Copy(plainBytes, padded, plainBytes.Length);
                for (int i = plainBytes.Length; i < padded.Length; i++)
                {
                    padded[i] = (byte)padLen;
                }

                using (var encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(padded, 0, padded.Length);
                }
            }
        }

        /// <summary>
        /// Get access token by username and password
        /// </summary>
        public async Task<(string? token, string? error)> GetAccessTokenAsync(string username, string password)
        {
            try
            {
                // Determine third_name based on username format
                string thirdName = username.Contains("@") ? "email" : "huami_phone";
                if (!username.Contains("@"))
                {
                    username = "+86" + username;
                }

                // Build request data
                var data = new Dictionary<string, string>
                {
                    { "emailOrPhone", username },
                    { "password", password },
                    { "state", "REDIRECTION" },
                    { "client_id", "HuaMi" },
                    { "country_code", "CN" },
                    { "token", "access" },
                    { "redirect_uri", "https://s3-us-west-2.amazonaws.com/hm-registration/successsignin.html" }
                };

                // Build query string
                string bodyPlain = string.Join("&", data.Select(kvp => 
                    $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));

                // Encrypt body
                byte[] bodyCipher = EncryptData(bodyPlain);

                // Build request
                var request = new HttpRequestMessage(HttpMethod.Post, LOGIN_URL)
                {
                    Content = new ByteArrayContent(bodyCipher)
                };

                // Set headers
                request.Headers.Add("Accept", "*/*");
                request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");
                request.Headers.Add("Connection", "keep-alive");
                request.Headers.Add("User-Agent", 
                    "Dalvik/2.1.0 (Linux; U; Android 9; MI 6 Build/PKQ1.190118.001) MiFit/4.6.0 (com.xiaomi.hm.health; build:46037; Android:28; androidBuild:PKQ1.190118.001)");
                request.Headers.Add("app_name", "com.xiaomi.hm.health");
                request.Headers.Add("appplatform", "android_phone");
                request.Headers.Add("x-hm-ekv", "1");
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

                // Send request
                var response = await _httpClient.SendAsync(request);
                var headers = response.Headers.ToString();
                var responseBody = await response.Content.ReadAsStringAsync();

                // Extract token from Location header
                if (response.Headers.Location != null)
                {
                    var location = response.Headers.Location.ToString();
                    var queryParams = HttpUtility.ParseQueryString(response.Headers.Location.Query);
                    
                    if (queryParams["access"] != null)
                    {
                        return (queryParams["access"], null);
                    }
                    
                    if (queryParams["refresh"] != null)
                    {
                        return (queryParams["refresh"], null);
                    }
                }

                // Try to extract from headers string
                var accessMatch = Regex.Match(headers, @"access=([^&\s]+)", RegexOptions.IgnoreCase);
                if (accessMatch.Success)
                {
                    return (accessMatch.Groups[1].Value, null);
                }

                var refreshMatch = Regex.Match(headers, @"refresh=([^&\s]+)", RegexOptions.IgnoreCase);
                if (refreshMatch.Success)
                {
                    return (refreshMatch.Groups[1].Value, null);
                }

                // Check for error
                if (headers.Contains("error="))
                {
                    return (null, "账号或密码错误！");
                }

                return (null, $"登录token接口请求失败，HTTP {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (null, $"请求异常: {ex.Message}");
            }
        }

        /// <summary>
        /// Test if apptoken is valid
        /// </summary>
        public async Task<bool> TestAppTokenAsync(string appToken)
        {
            try
            {
                var url = "https://api-mifit-cn3.zepp.com/v2/users/me/events";
                var request = new HttpRequestMessage(HttpMethod.Get, url + "?eventType=phn&limit=200");
                request.Headers.Add("User-Agent", "Zepp/9.13.0 (iPhone; iOS 26.1; Scale/3.00)");
                request.Headers.Add("apptoken", appToken);

                var response = await _httpClient.SendAsync(request);
                return response.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}

