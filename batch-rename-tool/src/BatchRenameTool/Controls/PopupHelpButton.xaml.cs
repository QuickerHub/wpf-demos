using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BatchRenameTool.Utils;
using DependencyPropertyGenerator;

namespace BatchRenameTool.Controls;

/// <summary>
/// Button control that shows a popup help window with Markdown content
/// </summary>
[DependencyProperty<string>("Title", DefaultValue = "使用帮助")]
[DependencyProperty<string>("ButtonContent", DefaultValue = "❓ 帮助")]
[DependencyProperty<string>("ResourcePath", DefaultValue = "Help.md")]
[DependencyProperty<string>("MarkdownContent")]
[DependencyProperty<double>("ScrollMultiplier", DefaultValue = 1.5)]
public partial class PopupHelpButton : UserControl
{
    public PopupHelpButton()
    {
        InitializeComponent();
        Loaded += PopupHelpButton_Loaded;
    }

    private void PopupHelpButton_Loaded(object sender, RoutedEventArgs e)
    {
        // Load markdown content from embedded resource
        LoadHelpContent();
    }

    private void LoadHelpContent()
    {
        if (!string.IsNullOrEmpty(ResourcePath))
        {
            try
            {
                MarkdownContent = ResourceLoader.ReadText(ResourcePath);
            }
            catch (FileNotFoundException)
            {
                // Resource not found, show error message
                MarkdownContent = $"# 错误\n\n无法加载帮助文档：{ResourcePath}";
            }
        }
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        HelpPopup.IsOpen = !HelpPopup.IsOpen;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        HelpPopup.IsOpen = false;
    }

    private void MarkdownViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Find the ScrollViewer inside MarkdownScrollViewer
        if (sender is DependencyObject obj)
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(obj);
            if (scrollViewer != null)
            {
                // Calculate new offset with increased scroll amount
                var newOffset = scrollViewer.VerticalOffset - (e.Delta * ScrollMultiplier);
                
                // Clamp to valid range
                newOffset = System.Math.Max(0, System.Math.Min(newOffset, scrollViewer.ScrollableHeight));
                
                // Scroll to new position
                scrollViewer.ScrollToVerticalOffset(newOffset);
                
                // Mark event as handled to prevent default scrolling
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Find a visual child of the specified type
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
            {
                return result;
            }
            
            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
            {
                return childOfChild;
            }
        }
        return null;
    }
}
