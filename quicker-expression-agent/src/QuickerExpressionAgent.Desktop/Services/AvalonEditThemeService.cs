using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Extensions.Hosting;
using Wpf.Ui.Appearance;

namespace QuickerExpressionAgent.Desktop.Services;

/// <summary>
/// Service to manage AvalonEdit TextEditor theme adaptation
/// Monitors WPF UI theme changes and automatically updates syntax highlighting colors
/// </summary>
public class AvalonEditThemeService : IHostedService
{
    private readonly HashSet<TextEditor> _registeredEditors = new();

    private static readonly string[] ColorNames = 
    { 
        "Keyword", "String", "Comment", "Number", "Type", "Method", 
        "Property", "Preprocessor", "XmlAttribute", "XmlAttributeValue",
        "XmlTag", "XmlComment", "XmlCData", "XmlEntity", "XmlText", "DefaultText"
    };

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Register theme change handler
        ApplicationThemeManager.Changed += (_, _) => ApplyThemeToAll();

        // Register global handler for TextEditor Loaded event
        EventManager.RegisterClassHandler(
            typeof(TextEditor),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, args) =>
            {
                if (sender is TextEditor editor)
                {
                    RegisterEditor(editor);
                }
            }));

        // Apply initial theme to any existing editors
        ApplyThemeToAll();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _registeredEditors.Clear();
        return Task.CompletedTask;
    }

    public void RegisterEditor(TextEditor editor)
    {
        if (editor == null) return;

        _registeredEditors.Add(editor);
        ApplyTheme(editor);
        editor.Unloaded += (_, _) => _registeredEditors.Remove(editor);
    }

    public void UnregisterEditor(TextEditor editor)
    {
        if (editor != null) _registeredEditors.Remove(editor);
    }

    public void ApplyTheme(TextEditor editor)
    {
        if (editor?.SyntaxHighlighting is not { Name: "C#" } highlighting) return;

        var themeType = ApplicationThemeManager.GetAppTheme();
        var resourceName = themeType == ApplicationTheme.Dark
            ? "QuickerExpressionAgent.Desktop.Resources.CSharp-Dark.xshd"
            : "QuickerExpressionAgent.Desktop.Resources.CSharp-Light.xshd";

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream == null) return;

        using var reader = new XmlTextReader(stream);
        var themeHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        ApplyColorsFromDefinition(highlighting, themeHighlighting);
    }

    private void ApplyThemeToAll()
    {
        foreach (var editor in _registeredEditors.ToList())
        {
            ApplyTheme(editor);
        }
    }

    private static void ApplyColorsFromDefinition(IHighlightingDefinition baseHighlighting, IHighlightingDefinition themeHighlighting)
    {
        foreach (var colorName in ColorNames)
        {
            try
            {
                var themeColor = themeHighlighting.GetNamedColor(colorName);
                var baseColor = baseHighlighting.GetNamedColor(colorName);
                
                if (themeColor?.Foreground != null && baseColor != null)
                {
                    baseColor.Foreground = themeColor.Foreground;
                    if (themeColor.FontWeight.HasValue)
                        baseColor.FontWeight = themeColor.FontWeight;
                }
            }
            catch
            {
                // Ignore errors for colors that don't exist
            }
        }
    }
}

