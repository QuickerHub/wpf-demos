using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Cea.Utils.Extension;

namespace Cea.Utils
{
    public static class HttpUtil
    {
        public static async Task<string?> TryGetContent(this HttpClient client, string url, int count = 2)
        {
            Exception? ee = null;
            for (var i = 0; i < count; i++)
            {
                try
                {
                    return await GetContent(client, url);
                }
                catch (Exception e)
                {
                    ee = e;
                }
            }

            if (ee != null) throw ee;
            return null;
        }

        public static async Task<string> GetContent(this HttpClient client, string url)
        {
            var response = await client.GetAsync(url);

            return response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync()
                : throw new Exception($"Request failed with status code: {response.StatusCode}");
        }

        public static async Task<string> PostWithForm(this HttpClient client, string url, object form1)
        {
            return await PostWithForm(client, url, form1.ConvertToDictionary());
        }

        public static async Task<string> PostWithForm(this HttpClient client, string url, Dictionary<string, object> form1)
        {
            var form2 = form1.Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value?.ToString());
            var response = await client.PostAsync(url, new FormUrlEncodedContent(form2));

            return response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync()
                : throw new Exception($"post failed with status code: {response.StatusCode}");
        }

        public static async Task<string> PostWithMultipartForm(this HttpClient client, string url, MultipartFormDataContent formData)
        {
            HttpResponseMessage response = await client.PostAsync(url, formData);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync()
                : throw new Exception($"POST request failed with status code: {response.StatusCode}");
        }

        public static void AddCookieFromString(this CookieContainer container, string cookie, string url)
        {
            if (container == null || string.IsNullOrEmpty(cookie))
                return;

            string[] cookiePairs = cookie.Split(';');

            foreach (string cookiePair in cookiePairs)
            {
                string[] parts = cookiePair.Trim().Split('=');

                if (parts.Length == 2)
                {
                    string name = parts[0].Trim();
                    string value = parts[1].Trim();
                    Uri uri = new(url);

                    container.Add(new Cookie(name, value, "/", uri.Host));
                }
            }
        }

        public static FileStream AddFile(this MultipartFormDataContent formData, string filepath, string fieldName = "file")
        {
            var fileStream = File.OpenRead(filepath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = fieldName.ToJson(),
                FileName = Path.GetFileName(filepath).ToJson(),
            };

            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(System.Web.MimeMapping.GetMimeMapping(filepath));

            formData.Add(fileContent);
            return fileStream;
        }

        public static void AddText(this MultipartFormDataContent formData, string name, string value)
        {
            formData.Add(new StringContent(value), name);
        }

        public static void AddTextFields(this MultipartFormDataContent formData, Dictionary<string, string> fields)
        {
            foreach (var field in fields)
            {
                formData.Add(new StringContent(field.Value), field.Key);
            }
        }
    }
}

