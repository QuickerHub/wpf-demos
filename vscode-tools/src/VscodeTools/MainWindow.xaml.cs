using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace VscodeTools
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Automatically show clipboard content when window is loaded
            ShowClipboardContent();
        }

        private void OpenSelectedFileButton_Click(object sender, RoutedEventArgs e)
        {
            VscodeFileOpener.TryOpenFileFromClipboard();
        }

        private void ShowClipboardContentButton_Click(object sender, RoutedEventArgs e)
        {
            ShowClipboardContent();
        }

        private void ShowClipboardContent()
        {
            var content = VscodeFileOpener.GetClipboardFileListContent();
            
            if (content == null)
            {
                MessageBox.Show(
                    "剪贴板中没有找到 code/file-list 格式的内容。",
                    "剪贴板内容",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    content,
                    "剪贴板内容 (code/file-list)",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void ShowAllFormatsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAllClipboardFormats();
        }

        private void ShowAllClipboardFormats()
        {
            try
            {
                var dataObject = Clipboard.GetDataObject();
                if (dataObject == null)
                {
                    MessageBox.Show(
                        "剪贴板为空或无法访问。",
                        "剪贴板格式",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var formats = dataObject.GetFormats();
                if (formats == null || formats.Length == 0)
                {
                    MessageBox.Show(
                        "剪贴板中没有找到任何格式。",
                        "剪贴板格式",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"找到 {formats.Length} 个格式：");
                sb.AppendLine();

                foreach (var format in formats.OrderBy(f => f))
                {
                    sb.AppendLine(format);
                    
                    // Try to get a preview of the data
                    try
                    {
                        var data = dataObject.GetData(format);
                        if (data != null)
                        {
                            if (data is string str)
                            {
                                var preview = str.Length > 100 ? str.Substring(0, 100) + "..." : str;
                                sb.AppendLine($"  -> {preview.Replace("\r", "\\r").Replace("\n", "\\n")}");
                            }
                            else if (data is string[] arr)
                            {
                                sb.AppendLine($"  -> 字符串数组，共 {arr.Length} 项");
                                if (arr.Length > 0 && arr[0] is string firstItem)
                                {
                                    var preview = firstItem.Length > 50 ? firstItem.Substring(0, 50) + "..." : firstItem;
                                    sb.AppendLine($"     第一项: {preview}");
                                }
                            }
                            else if (data is MemoryStream ms)
                            {
                                // Try to read MemoryStream as text
                                try
                                {
                                    var originalPosition = ms.Position;
                                    ms.Position = 0;
                                    
                                    // Try to read as UTF-8 text
                                    byte[] buffer = new byte[ms.Length];
                                    int bytesRead = ms.Read(buffer, 0, buffer.Length);
                                    ms.Position = originalPosition; // Restore original position
                                    
                                    if (bytesRead > 0)
                                    {
                                        // Try UTF-8 first
                                        string content = null;
                                        try
                                        {
                                            content = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                        }
                                        catch
                                        {
                                            // If UTF-8 fails, try default encoding
                                            try
                                            {
                                                content = Encoding.Default.GetString(buffer, 0, bytesRead);
                                            }
                                            catch
                                            {
                                                // If all fails, show as hex
                                                content = null;
                                            }
                                        }
                                        
                                        if (!string.IsNullOrEmpty(content) && !content.Any(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t'))
                                        {
                                            var preview = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
                                            sb.AppendLine($"  -> MemoryStream 内容 ({bytesRead} 字节):");
                                            sb.AppendLine($"     {preview.Replace("\r", "\\r").Replace("\n", "\\n")}");
                                        }
                                        else
                                        {
                                            sb.AppendLine($"  -> 类型: MemoryStream (长度: {ms.Length} 字节，二进制数据)");
                                        }
                                    }
                                    else
                                    {
                                        sb.AppendLine($"  -> 类型: MemoryStream (空)");
                                    }
                                }
                                catch
                                {
                                    sb.AppendLine($"  -> 类型: MemoryStream (长度: {ms.Length} 字节)");
                                }
                            }
                            else
                            {
                                sb.AppendLine($"  -> 类型: {data.GetType().Name}");
                            }
                        }
                    }
                    catch
                    {
                        sb.AppendLine("  -> (无法读取数据)");
                    }
                    
                    sb.AppendLine();
                }

                MessageBox.Show(
                    sb.ToString(),
                    "剪贴板所有格式",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"获取剪贴板格式时出错：{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}

