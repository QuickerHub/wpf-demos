using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace QuickerActionManage.Utils
{
    /// <summary>
    /// Static utility class for loading resources and XAML files
    /// </summary>
    public static class Loader
    {
        // Thread-safe collection to track loaded resources
        private static readonly ConcurrentDictionary<string, bool> LoadedResources = new();

        /// <summary>
        /// Creates pack URI for resource
        /// </summary>
        private static Uri CreatePackUri(string path, Assembly assembly)
        {
            string assemblyName = assembly.GetName().Name!;
            return new Uri($"pack://application:,,,/{assemblyName};component/{path}");
        }

        /// <summary>
        /// Reads text content from embedded resource
        /// </summary>
        /// <param name="path">Resource path (e.g., "aaa/bbb/ccc")</param>
        /// <param name="assembly">Assembly containing the resource (optional)</param>
        /// <returns>Text content of the resource</returns>
        public static string ReadText(string path, Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();

            var resourceInfo = Application.GetResourceStream(CreatePackUri(path, assembly));
            if (resourceInfo?.Stream == null)
                throw new FileNotFoundException($"Resource not found: {path}");

            using var stream = resourceInfo.Stream;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Loads a XAML resource dictionary
        /// </summary>
        /// <param name="path">XAML resource path (e.g., "Themes/MyTheme.xaml")</param>
        /// <param name="assembly">Assembly containing the XAML resource</param>
        /// <returns>True if loaded successfully or already loaded</returns>
        public static bool LoadXaml(string path, Assembly assembly)
        {
            string resourceKey = $"{assembly.GetName().Name}:{path}";

            // Return true if already loaded
            if (LoadedResources.ContainsKey(resourceKey))
                return true;

            try
            {
                var resourceDictionary = new ResourceDictionary
                {
                    Source = CreatePackUri(path, assembly)
                };

                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
                LoadedResources[resourceKey] = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Loads multiple theme XAML files
        /// </summary>
        /// <param name="assembly">Assembly containing the XAML resources</param>
        /// <param name="paths">Array of XAML resource paths</param>
        /// <returns>True if all resources loaded successfully</returns>
        public static bool LoadThemeXamls(Assembly assembly, params string[] paths)
        {
            bool success = true;

            if (paths != null)
            {
                success = paths.All(path => LoadXaml(path, assembly));
            }

            return success;
        }

        /// <summary>
        /// Checks if a resource has been loaded
        /// </summary>
        public static bool IsLoaded(string path, Assembly assembly)
        {
            string resourceKey = $"{assembly.GetName().Name}:{path}";
            return LoadedResources.ContainsKey(resourceKey);
        }

        /// <summary>
        /// Clears the loaded resources cache
        /// </summary>
        public static void ClearCache()
        {
            LoadedResources.Clear();
        }
    }
}

