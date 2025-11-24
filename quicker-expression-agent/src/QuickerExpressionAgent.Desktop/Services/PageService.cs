using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Abstractions;

namespace QuickerExpressionAgent.Desktop.Services;

/// <summary>
/// Service that provides pages for navigation.
/// Implements INavigationViewPageProvider for WPF-UI 4.0.3
/// </summary>
public class PageService : INavigationViewPageProvider
{
    /// <summary>
    /// Service which provides the instances of pages.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates new instance and attaches the <see cref="IServiceProvider"/>.
    /// </summary>
    public PageService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public object? GetPage(Type pageType)
    {
        if (!typeof(FrameworkElement).IsAssignableFrom(pageType))
            throw new InvalidOperationException("The page should be a WPF control.");

        // Use GetRequiredService to ensure we get the registered Singleton instance
        return _serviceProvider.GetRequiredService(pageType);
    }
}

