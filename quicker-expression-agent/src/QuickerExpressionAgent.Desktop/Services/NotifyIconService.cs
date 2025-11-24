using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Wpf.Ui.Tray;
using Wpf.Ui.Tray.Controls;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using MenuItem = System.Windows.Controls.MenuItem;

namespace QuickerExpressionAgent.Desktop.Services;

/// <summary>
/// Service for managing system tray icon using WPF-UI.Tray
/// </summary>
public class NotifyIconService : Wpf.Ui.Tray.NotifyIconService, IDisposable
{
    private readonly ContextMenu _menu;
    private readonly ILogger<NotifyIconService>? _logger;
    private readonly MainWindowService _mainWindowService;
    private bool _disposed;

    public NotifyIconService(MainWindowService mainWindowService, ILogger<NotifyIconService>? logger = null)
    {
        _logger = logger;
        _mainWindowService = mainWindowService;
        _menu = new ContextMenu();
        TooltipText = "QuickerAgent";
        
        // Initialize menu items
        UpdateMenuItems();
    }

    /// <summary>
    /// Show the tray icon
    /// The NotifyIcon in XAML will auto-register itself, this method is just for compatibility
    /// </summary>
    public void Show()
    {
        if (_disposed)
            return;

        // The NotifyIcon in XAML will auto-register itself
        // We just need to ensure the menu is set up (done in MainWindow.Loaded)
        _logger?.LogInformation("Tray icon service initialized");
    }

    /// <summary>
    /// Get the context menu for NotifyIcon
    /// </summary>
    public ContextMenu GetMenu()
    {
        return _menu;
    }

    /// <summary>
    /// Update menu items in the context menu
    /// </summary>
    public void UpdateMenuItems()
    {
        _menu.Items.Clear();
        foreach (var item in GetMenuItems())
        {
            if (item is MenuItem menuItem)
            {
                menuItem.Click += OnMenuItemClick;
            }
            _menu.Items.Add(item);
        }
    }

    /// <summary>
    /// Get menu items for the tray icon context menu
    /// </summary>
    protected virtual IEnumerable<object> GetMenuItems()
    {
        yield return new MenuItem
        {
            Header = "显示窗口",
            Icon = new SymbolIcon(SymbolRegular.Window20),
        };
        yield return new Separator();
        yield return new MenuItem
        {
            Header = "退出",
            Icon = new SymbolIcon(SymbolRegular.ArrowExit20),
        };
    }

    /// <summary>
    /// Handle right click on tray icon
    /// Note: This method may not be called if NotifyIcon is used directly in XAML
    /// The menu will be shown automatically if MenuOnRightClick="True" is set
    /// </summary>
    protected override void OnRightClick()
    {
        // Update menu items (in case they changed)
        UpdateMenuItems();
        _logger?.LogInformation("Tray icon right clicked");
    }

    /// <summary>
    /// Handle left click on tray icon
    /// </summary>
    protected override void OnLeftClick()
    {
        ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        try
        {
            _mainWindowService.ShowAndNavigate<QuickerExpressionAgent.Desktop.Pages.ExpressionGeneratorPage>();
            _logger?.LogInformation("Tray icon clicked - showing main window");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to show main window");
        }
    }

    private void OnMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            var header = menuItem.Header?.ToString();
            if (header == "显示窗口")
            {
                ShowMainWindow();
            }
            else if (header == "退出")
            {
                Application.Current.Shutdown();
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Base class will handle disposal
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
