using System;
using System.IO;
using System.Reflection;

namespace WebViewMarkdownTip
{
    /// <summary>
    /// Configuration for WebView2 integration (dev server vs packaged Web folder).
    /// </summary>
    public class WebViewConfiguration
    {
        public const string DefaultDevServerUrl = "http://localhost:5174";

        public const string DefaultVirtualHostName = "app.markdowntip.local";

        public const string DefaultDevServerInfoFile = ".vite-dev-server";

        public const string DefaultWebEntryFile = "index.html";

        public bool IsDebugMode { get; }

        public string DevServerUrl { get; }

        public string WebBasePath { get; }

        public string VirtualHostName { get; }

        public string WebEntryFile { get; }

        public string DevServerInfoFile { get; }

        public string UserDataFolder { get; }

        public WebViewConfiguration()
            : this(null, null, null)
        {
        }

        public WebViewConfiguration(
            string? devServerUrl = null,
            string? virtualHostName = null,
            string? webEntryFile = null)
        {
#if DEBUG
            IsDebugMode = true;
#else
            IsDebugMode = false;
#endif

            DevServerUrl = Environment.GetEnvironmentVariable("WPF_MARKDOWN_TIP_DEV_URL")
                        ?? Environment.GetEnvironmentVariable("WPF_WEBVIEW_DEV_URL")
                        ?? devServerUrl
                        ?? DefaultDevServerUrl;

            var assembly = Assembly.GetExecutingAssembly();
            var assemblyDirectory = Path.GetDirectoryName(assembly.Location);
            WebBasePath = Path.GetFullPath(Path.Combine(assemblyDirectory ?? ".", "Web"));

            UserDataFolder = Path.GetFullPath(Path.Combine(assemblyDirectory ?? ".", "WebView2Data"));

            VirtualHostName = virtualHostName ?? DefaultVirtualHostName;

            WebEntryFile = webEntryFile ?? DefaultWebEntryFile;

            DevServerInfoFile = DefaultDevServerInfoFile;
        }

        public string? GetDevServerUrlFromFile()
        {
            try
            {
                var filePath = Path.Combine(WebBasePath, DevServerInfoFile);

                if (File.Exists(filePath))
                {
                    var url = File.ReadAllText(filePath).Trim();
                    if (!string.IsNullOrEmpty(url))
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"WebViewConfiguration: Found dev server URL in file: {url} (from {filePath})");
                        return url;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"WebViewConfiguration: Error reading dev server file: {ex.Message}");
            }

            return null;
        }

        public string GetLocalWebFilePath()
        {
            return Path.Combine(WebBasePath, WebEntryFile);
        }

        public string GetVirtualUrl()
        {
            return $"http://{VirtualHostName}/{WebEntryFile}";
        }
    }
}
