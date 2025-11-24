using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using QuickerExpressionAgent.Quicker.Services;

namespace QuickerExpressionAgent.Quicker;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly QuickerServiceServer? _serviceServer;
    private readonly IServiceProvider _serviceProvider;
    private readonly DotNetVersionChecker _dotNetVersionChecker;
    private readonly Services.DesktopServiceClientConnector? _desktopServiceClientConnector;

    [ObservableProperty]
    private string _connectionStatus = "等待连接...";

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private string _dotNetVersionStatus = "检查中...";

    [ObservableProperty]
    private bool _isDotNet80Installed = false;

    public MainWindowViewModel(QuickerServiceServer serviceServer, IServiceProvider serviceProvider)
    {
        _serviceServer = serviceServer ?? throw new ArgumentNullException(nameof(serviceServer));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _dotNetVersionChecker = serviceProvider.GetRequiredService<DotNetVersionChecker>();
        _desktopServiceClientConnector = serviceProvider.GetService<Services.DesktopServiceClientConnector>();
        _serviceServer.ConnectionStatusChanged += OnConnectionStatusChanged;
        UpdateConnectionStatus(_serviceServer.IsClientConnected);
        CheckDotNetVersion();
    }

    private void CheckDotNetVersion()
    {
        IsDotNet80Installed = _dotNetVersionChecker.IsDotNet80Installed();
        var installedVersion = _dotNetVersionChecker.GetInstalledVersion();
        
        if (IsDotNet80Installed)
        {
            DotNetVersionStatus = $"已安装 .NET {installedVersion ?? "8.0+"}";
        }
        else if (!string.IsNullOrEmpty(installedVersion))
        {
            DotNetVersionStatus = $"已安装 .NET {installedVersion}（需要 8.0+）";
        }
        else
        {
            DotNetVersionStatus = "未安装 .NET 8.0+";
        }
    }

    private void OnConnectionStatusChanged(object? sender, bool isConnected)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateConnectionStatus(isConnected);
        });
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        IsConnected = isConnected;
        ConnectionStatus = isConnected ? "已连接" : "等待连接...";
    }

    [RelayCommand]
    private void OpenTestWindow()
    {
        var testWindow = _serviceProvider.GetRequiredService<QuickerServiceTestWindow>();
        testWindow.Show();
    }

    [RelayCommand]
    private async Task StartDesktop()
    {
        try
        {
            var runner = new Runner();
            await runner.OpenChatWindowAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动 Desktop 应用失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void OpenDotNetInstallWindow()
    {
        var downloadUrl = _dotNetVersionChecker.GetDownloadUrl();
        var installWindow = new DotNetInstallWindow(downloadUrl);
        installWindow.ShowDialog();
    }

    [RelayCommand]
    private async Task ShutdownDesktop()
    {
        try
        {
            if (_desktopServiceClientConnector == null)
            {
                MessageBox.Show("Desktop 服务连接器未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_desktopServiceClientConnector.IsConnected)
            {
                MessageBox.Show("Desktop 应用未连接", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = await _desktopServiceClientConnector.ServiceClient.ShutdownAsync();
            if (result)
            {
                MessageBox.Show("Desktop 应用已退出", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("退出 Desktop 应用失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"退出 Desktop 应用时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

