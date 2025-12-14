using System;
using System.IO;
using System.Reflection;

namespace WpfWebview
{
    /// <summary>
    /// Configuration for WebView2 integration
    /// Provides unified configuration for development and production environments
    /// </summary>
    public class WebViewConfiguration
    {
        /// <summary>
        /// Default development server URL
        /// </summary>
        public const string DefaultDevServerUrl = "http://localhost:5173";

        /// <summary>
        /// Default virtual host name for local file loading
        /// </summary>
        public const string DefaultVirtualHostName = "app.webview.local";

        /// <summary>
        /// Default dev server info file name
        /// </summary>
        public const string DefaultDevServerInfoFile = ".vite-dev-server";

        /// <summary>
        /// Default web entry file name
        /// </summary>
        public const string DefaultWebEntryFile = "index.html";

        /// <summary>
        /// Whether running in debug mode
        /// </summary>
        public bool IsDebugMode { get; }

        /// <summary>
        /// Development server URL
        /// </summary>
        public string DevServerUrl { get; }

        /// <summary>
        /// Base path for web files (output directory)
        /// </summary>
        public string WebBasePath { get; }

        /// <summary>
        /// Virtual host name for local file loading
        /// </summary>
        public string VirtualHostName { get; }

        /// <summary>
        /// Web entry file name (e.g., index.html)
        /// </summary>
        public string WebEntryFile { get; }

        /// <summary>
        /// Path to dev server info file name
        /// </summary>
        public string DevServerInfoFile { get; }

        /// <summary>
        /// User data folder for WebView2 (in assembly directory)
        /// </summary>
        public string UserDataFolder { get; }

        /// <summary>
        /// Create configuration with default settings
        /// </summary>
        public WebViewConfiguration()
            : this(null, null, null)
        {
        }

        /// <summary>
        /// Create configuration with custom settings
        /// </summary>
        /// <param name="devServerUrl">Development server URL (default: http://localhost:5173)</param>
        /// <param name="virtualHostName">Virtual host name for local files (default: app.webview.local)</param>
        /// <param name="webEntryFile">Web entry file name (default: index.html)</param>
        public WebViewConfiguration(
            string? devServerUrl = null,
            string? virtualHostName = null,
            string? webEntryFile = null)
        {
            // Check if running in debug mode
            #if DEBUG
            IsDebugMode = true;
            #else
            IsDebugMode = false;
            #endif

            // Get dev server URL from environment variable or parameter, fallback to default
            DevServerUrl = Environment.GetEnvironmentVariable("WPF_WEBVIEW_DEV_URL")
                        ?? devServerUrl
                        ?? DefaultDevServerUrl;

            var assembly = Assembly.GetExecutingAssembly();
            var assemblyDirectory = Path.GetDirectoryName(assembly.Location);
            WebBasePath = Path.GetFullPath(Path.Combine(assemblyDirectory, "Web"));

            // Set user data folder in assembly directory to avoid permission issues
            UserDataFolder = Path.GetFullPath(Path.Combine(assemblyDirectory, "WebView2Data"));

            // Set virtual host name
            VirtualHostName = virtualHostName ?? DefaultVirtualHostName;

            // Set web entry file
            WebEntryFile = webEntryFile ?? DefaultWebEntryFile;

            // Set dev server info file
            DevServerInfoFile = DefaultDevServerInfoFile;
        }

        /// <summary>
        /// Get dev server URL from info file in Web folder
        /// Only checks the Web folder in output directory
        /// </summary>
        public string? GetDevServerUrlFromFile()
        {
            try
            {
                // Check Web folder (where web files are copied)
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

        /// <summary>
        /// Get local web file path
        /// </summary>
        public string GetLocalWebFilePath()
        {
            return Path.Combine(WebBasePath, WebEntryFile);
        }

        /// <summary>
        /// Get virtual URL for local file loading
        /// </summary>
        public string GetVirtualUrl()
        {
            return $"http://{VirtualHostName}/{WebEntryFile}";
        }
    }
}

