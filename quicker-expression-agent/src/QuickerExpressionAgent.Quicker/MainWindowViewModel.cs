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

    public MainWindowViewModel(QuickerServiceServer serviceServer)
    {
        _serviceServer = serviceServer ?? throw new ArgumentNullException(nameof(serviceServer));
        _serviceServer.ConnectionStatusChanged += OnConnectionStatusChanged;
        UpdateConnectionStatus(_serviceServer.IsClientConnected);
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
}

