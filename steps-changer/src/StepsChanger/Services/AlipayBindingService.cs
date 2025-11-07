using System;
using System.Collections.Generic;
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
    /// Alipay binding service for binding Alipay account to Zepp
    /// </summary>
    public class AlipayBindingService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private string? _cookies;

        public AlipayBindingService()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Get Alipay authorization URL
        /// </summary>
        public async Task<string?> GetAlipayAuthUrlAsync(string appToken, string userId)
        {
            try
            {
                var url = "https://api-mifit-cn3.zepp.com/v1/thirdParties/auth.json";
                var request = new HttpRequestMessage(HttpMethod.Get, url + $"?userid={userId}");
                
                request.Headers.Add("Host", "api-mifit-cn3.zepp.com");
                request.Headers.Add("User-Agent", "Zepp/9.13.0 (iPhone; iOS 26.1; Scale/3.00)");
                request.Headers.Add("Connection", "keep-alive");
                request.Headers.Add("appname", "com.huami.midong");
                request.Headers.Add("apptoken", appToken);
                request.Headers.Add("Accept", "*/*");
                request.Headers.Add("Accept-Encoding", "gzip, deflate, br");

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);

                if (data["data"]?["authInfo"] != null)
                {
                    string authInfo = data["data"]["authInfo"].ToString();
                    return BuildAlipayUrl(authInfo);
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"获取支付宝授权URL失败: {ex.Message}");
            }
        }

        /// <summary>
        /// Build Alipay authorization URL from authInfo
        /// </summary>
        private string BuildAlipayUrl(string infoStr)
        {
            var paramsDict = HttpUtility.ParseQueryString(infoStr);
            var url = new StringBuilder("https://authweb.alipay.com/auth?v=h5");

            var keys = new[] { "app_id", "sign", "biz_type", "auth_type", "apiname", "scope", "target_id", "product_id", "pid" };
            foreach (var key in keys)
            {
                var value = paramsDict[key];
                if (!string.IsNullOrEmpty(value))
                {
                    url.Append($"&{key}={HttpUtility.UrlEncode(value)}");
                }
            }

            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            url.Append($"&mqpNotifyName=CashierAuth_{timestamp}");
            url.Append($"&clientTraceId={timestamp}");
            url.Append("&bundle_id=com.huami.watch");
            url.Append("&app_name=mc");
            url.Append("&msp_type=embeded-ios");
            url.Append("&method=");

            return url.ToString();
        }

        /// <summary>
        /// Simulate Alipay authorization process
        /// </summary>
        public async Task<(string? contextToken, string? updatedCookie, string? traceId, string? url)> SimulateAlipayAuthAsync(
            string authUrl, string cookies)
        {
            _cookies = cookies;

            try
            {
                // Extract traceId from URL
                var uri = new Uri(authUrl);
                var queryParams = HttpUtility.ParseQueryString(uri.Query);
                string traceId = queryParams["clientTraceId"] ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                // Generate sign
                long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 300;
                string apdidToken = ExtractApdidToken(cookies);
                if (string.IsNullOrEmpty(apdidToken))
                {
                    throw new Exception("无法提取 apdidToken");
                }

                string sign = GenerateSign(ts.ToString(), traceId, apdidToken);

                // Build request
                var request = new HttpRequestMessage(HttpMethod.Get, authUrl);
                request.Headers.Add("User-Agent", 
                    "Mozilla/5.0 (iPhone; CPU iPhone OS 26_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/23B5073a Ariver/1.1.0 AliApp(AP/10.6.80.6000) Nebula WK RVKType(1) AlipayDefined(nt:WIFI,ws:393|788|3.0) AlipayClient/10.6.80.6000 Language/zh-Hans Region/CN NebulaX/1.0.0 DTN/2.0");
                request.Headers.Add("ts", ts.ToString());
                request.Headers.Add("sign", sign);
                request.Headers.Add("Cookie", cookies);
                request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                request.Headers.Add("Accept-Encoding", "gzip");
                request.Headers.Add("Accept-Language", "zh-CN,en-US;q=0.8");
                request.Headers.Add("Connection", "Keep-Alive");
                request.Headers.Add("Host", "authweb.alipay.com");

                var response = await _httpClient.SendAsync(request);
                var html = await response.Content.ReadAsStringAsync();

                // Check for login page
                if (html.Contains("<title>登录</title>"))
                {
                    return (null, null, null, null);
                }

                // Update cookies
                if (response.Headers.Contains("Set-Cookie"))
                {
                    var setCookies = response.Headers.GetValues("Set-Cookie");
                    foreach (var cookie in setCookies)
                    {
                        if (cookie.Contains(";"))
                        {
                            var cookieItem = cookie.Split(';')[0].Trim();
                            if (cookieItem.Contains("="))
                            {
                                var parts = cookieItem.Split(new[] { '=' }, 2);
                                if (parts.Length == 2)
                                {
                                    _cookies = UpdateCookie(_cookies, parts[0], parts[1]);
                                }
                            }
                        }
                    }
                }

                // Extract contextToken
                var contextMatch = Regex.Match(html, @"<script>window\.context\s*=\s*(.+?);</script>", RegexOptions.Singleline);
                if (contextMatch.Success)
                {
                    try
                    {
                        var jsonStr = contextMatch.Groups[1].Value.Trim();
                        var contextData = JObject.Parse(jsonStr);
                        var contextToken = contextData["contextToken"]?.ToString();

                        if (!string.IsNullOrEmpty(contextToken))
                        {
                            return (contextToken, _cookies, traceId, authUrl);
                        }
                    }
                    catch
                    {
                        // JSON parse error
                    }
                }

                return (null, null, null, null);
            }
            catch (Exception ex)
            {
                throw new Exception($"模拟支付宝授权失败: {ex.Message}");
            }
        }

        /// <summary>
        /// Submit Alipay authorization
        /// </summary>
        public async Task<Dictionary<string, string>?> SubmitAlipayAuthAsync(
            string contextToken, string updatedCookie, string traceId, string refererUrl)
        {
            try
            {
                var cookieDict = ParseCookies(updatedCookie);
                string ctoken = cookieDict.ContainsKey("ctoken") ? cookieDict["ctoken"] : "";

                var url = "https://authweb.alipay.com/auth";
                var envData = new
                {
                    bioMetaInfo = "4.12.0:1628342034496,32774,2088802520255575",
                    appVersion = "10.6.80.6000",
                    appName = "com.alipay.iphoneclient",
                    deviceType = "ios",
                    osVersion = "iOS 26.1",
                    viSdkVersion = "3.6.80.100",
                    deviceModel = "iPhone15,4"
                };

                var data = new Dictionary<string, string>
                {
                    { "contextToken", contextToken },
                    { "oauthScene", "AUTHACCOUNT" },
                    { "ctoken", ctoken },
                    { "token", "undefined" },
                    { "mqpNotifyName", $"CashierAuth_{traceId}" },
                    { "envData", Newtonsoft.Json.JsonConvert.SerializeObject(envData) },
                    { "channel", "SECURITYPAY" }
                };

                var content = new FormUrlEncodedContent(data);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                request.Headers.Add("Sec-Fetch-Dest", "empty");
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("Connection", "Keep-Alive");
                request.Headers.Add("Accept-Encoding", "gzip");
                request.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                request.Headers.Add("X-Alipay-Client-Session", "check");
                request.Headers.Add("Sec-Fetch-Site", "same-origin");
                request.Headers.Add("Origin", "https://authweb.alipay.com");
                request.Headers.Add("User-Agent",
                    "Mozilla/5.0 (iPhone; CPU iPhone OS 26_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/23B5073a Ariver/1.1.0 AliApp(AP/10.6.80.6000) Nebula WK RVKType(1) AlipayDefined(nt:WIFI,ws:393|788|3.0) AlipayClient/10.6.80.6000 Language/zh-Hans Region/CN NebulaX/1.0.0 DTN/2.0");
                request.Headers.Add("Sec-Fetch-Mode", "cors");
                request.Headers.Add("Cookie", updatedCookie);
                request.Headers.Add("Referer", refererUrl);
                request.Headers.Add("Host", "authweb.alipay.com");
                request.Headers.Add("x-allow-afts-limit", "true");
                request.Headers.Add("Accept", "*/*");

                var response = await _httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var responseData = JObject.Parse(responseText);
                    if (responseData["authDestUrl"] != null)
                    {
                        var authDestUrl = responseData["authDestUrl"].ToString();
                        var uri = new Uri(authDestUrl);
                        var queryParams = HttpUtility.ParseQueryString(uri.Query);

                        if (queryParams["result"] != null)
                        {
                            string resultB64 = queryParams["result"];
                            byte[] decodedBytes = Convert.FromBase64String(resultB64);
                            string decodedResult = Encoding.UTF8.GetString(decodedBytes);
                            decodedResult = HttpUtility.UrlDecode(decodedResult);

                            var resultParams = HttpUtility.ParseQueryString(decodedResult);
                            var result = new Dictionary<string, string>();
                            foreach (string key in resultParams.AllKeys)
                            {
                                result[key] = resultParams[key];
                            }
                            return result;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"提交支付宝授权失败: {ex.Message}");
            }
        }

        /// <summary>
        /// Bind Alipay account to Zepp
        /// </summary>
        public async Task<JObject?> BindAlipayAccountAsync(string authCode, string userId, string appToken)
        {
            try
            {
                var url = $"https://api-mifit-cn3.zepp.com/v1/thirdParties/auth.json?r={Guid.NewGuid()}";
                var data = new Dictionary<string, string>
                {
                    { "authCode", authCode },
                    { "userid", userId }
                };

                var content = new FormUrlEncodedContent(data);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString().ToUpper());
                request.Headers.Add("Host", "api-mifit-cn3.zepp.com");
                request.Headers.Add("lang", "zh_CN");
                request.Headers.Add("appplatform", "ios_phone");
                request.Headers.Add("country", "CN");
                request.Headers.Add("channel", "appstore");
                request.Headers.Add("Connection", "keep-alive");
                request.Headers.Add("hm-privacy-ceip", "false");
                request.Headers.Add("Accept-Language", "zh-Hans-CN;q=1, en-CN;q=0.9");
                request.Headers.Add("User-Agent", "ZeppLife/6.14.0 (iPhone; iOS 26.1; Scale/3.00)");
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
                request.Headers.Add("v", "2.0");
                request.Headers.Add("appname", "com.huami.midong");
                request.Headers.Add("Accept", "*/*");
                request.Headers.Add("timezone", "Asia/Shanghai");
                request.Headers.Add("cv", "319_6.14.0");
                request.Headers.Add("hm-privacy-diagnostics", "false");
                request.Headers.Add("apptoken", appToken);

                var response = await _httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return JObject.Parse(responseText);
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"绑定支付宝账号失败: {ex.Message}");
            }
        }

        private string ExtractApdidToken(string cookieStr)
        {
            var parts = cookieStr.Split(';');
            foreach (var part in parts)
            {
                if (part.Contains("devKeySet"))
                {
                    try
                    {
                        var jsonStr = part.Split(new[] { '=' }, 2)[1];
                        var data = JObject.Parse(jsonStr);
                        return data["apdidToken"]?.ToString() ?? "";
                    }
                    catch
                    {
                        // Parse error
                    }
                }
            }
            return "";
        }

        private string GenerateSign(string ts, string traceId, string apdidToken)
        {
            string raw = $"{ts}{traceId}{apdidToken}";
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private Dictionary<string, string> ParseCookies(string cookieStr)
        {
            var dict = new Dictionary<string, string>();
            var parts = cookieStr.Split(';');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Contains("="))
                {
                    var kvp = trimmed.Split(new[] { '=' }, 2);
                    if (kvp.Length == 2)
                    {
                        dict[kvp[0].Trim()] = kvp[1].Trim();
                    }
                }
            }
            return dict;
        }

        private string UpdateCookie(string cookies, string key, string value)
        {
            var dict = ParseCookies(cookies);
            dict[key] = value;
            return string.Join("; ", dict.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}

