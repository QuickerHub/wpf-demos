using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace QuickerExpressionAgent.Quicker;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly QuickerServiceServer? _serviceServer;

    [ObservableProperty]
    private string _connectionStatus = "等待连接...";

    [ObservableProperty]
    private bool _isConnected = false;

    public MainWindowViewModel()
    {
        try
        {
            _serviceServer = Launcher.GetService<QuickerServiceServer>();
            _serviceServer.ConnectionStatusChanged += OnConnectionStatusChanged;
            UpdateConnectionStatus(_serviceServer.IsClientConnected);
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"错误: {ex.Message}";
            throw;
        }
    }

    private void OnConnectionStatusChanged(object? sender, bool isConnected)
    {
        UpdateConnectionStatus(isConnected);
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        IsConnected = isConnected;
        ConnectionStatus = isConnected ? "已连接" : "等待连接...";
    }
}

