using System;
using System.Windows;

/// <summary>
/// Merges HandyControl + local markdown/button templates (replaces CeaQuicker Loader.LoadThemeXamls for this assembly).
/// </summary>
public static class ViewRunnerResourceLoader
{
    private static readonly object Gate = new();
    private static bool _merged;

    public static void EnsureMerged()
    {
        lock (Gate)
        {
            if (_merged)
            {
                return;
            }

            var app = System.Windows.Application.Current;
            if (app == null)
            {
                return;
            }

            var asm = typeof(ViewRunnerResourceLoader).Assembly;
            var name = asm.GetName().Name ?? "CeaViewRunner";
            var uri = new Uri($"pack://application:,,,/{name};component/Theme/CeaViewRunnerResources.xaml", UriKind.Absolute);
            var dict = new ResourceDictionary { Source = uri };
            app.Resources.MergedDictionaries.Insert(0, dict);
            _merged = true;
        }
    }
}
