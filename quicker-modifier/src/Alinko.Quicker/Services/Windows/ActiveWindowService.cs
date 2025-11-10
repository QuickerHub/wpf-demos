using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using Windows.Win32;
using System.Windows;
using System.Windows.Interop;

namespace Alinko.Quicker.Services.Windows;

public record WindowChangeState
{
    public nint HWnd { get; set; }

    public WindowChangeState(nint hWnd)
    {
        HWnd = hWnd;
    }

    public DateTime ActivateTime { get; set; } = DateTime.Now;
}
public interface IActiveWindowService
{
    void Start();
    IntPtr GetLastForegroundWindow(IntPtr? except = null);
    void Ignore(IntPtr hWND);

    /// <summary>
    /// Event fired when foreground window changes with window handle and change type
    /// </summary>
    event ForegroundWindowChangedEventHandler? ForegroundWindowChanged;
}
/// <summary>
/// use <see cref="Start"/> to be started manually
/// </summary>
public class ActiveWindowService : IActiveWindowService, IDisposable
{
    private readonly ActiveWindowHook hook;
    private readonly SourceCache<WindowChangeState, nint> cache;
    private readonly ILogger logger;

    /// <summary>
    /// Event fired when foreground window changes with window handle and change type
    /// </summary>
    public event ForegroundWindowChangedEventHandler? ForegroundWindowChanged;

    public ActiveWindowService(ILogger<ActiveWindowService> logger)
    {
        this.logger = logger;
        hook = new();
        hook.ForegroundWindowChanged += Hook_ForegroundWindowChanged;

        cache = new(x => x.HWnd);

        cache.Connect()
             .SortBy(x => x.ActivateTime, SortDirection.Descending)
             .Bind(out activeWindows)
             .Subscribe();

        activeWindows.ObserveCollectionChanges()
            .Throttle(TimeSpan.FromSeconds(10))
            .Where(_ => activeWindows.Count > 100)
            .Subscribe(_ => cache.Remove(activeWindows.Skip(100).Select(x => x.HWnd).ToList()));
    }
    public void Start()
    {
        hook.StartHook();
        logger.LogInformation("ActiveWindowService started");
    }

    public ReadOnlyObservableCollection<WindowChangeState> activeWindows;

    public nint GetLastForegroundWindow(nint? except = null)
    {
        return activeWindows.FirstOrDefault(x => x.HWnd != except)?.HWnd ?? IntPtr.Zero;
    }

    private readonly HashSet<nint> _ignoreSet = [];
    public void Ignore(nint hWND)
    {
        _ignoreSet.Add(hWND);
        cache.Remove(hWND);
    }

    private void Update(nint hWND)
    {
        if (!_ignoreSet.Contains(hWND))
        {
            cache.AddOrUpdate(new WindowChangeState(hWND));
        }
    }

    private void Hook_ForegroundWindowChanged(object sender, ForegroundWindowChangedEventArgs e)
    {
        //logger.LogInformation("ForegroundWindowChanged {0}, {1}", e.ChangeType, WinProperty.GetForeground().GetSummary());
        //var handle = PInvoke.GetForegroundWindow();
        switch (e.ChangeType)
        {
            case WindowChangeType.Foreground:
            case WindowChangeType.CaptureStart: // Mouse capture window
            case WindowChangeType.MinimizeEnd: // Minimize end
                try
                {
                    var winp = WinProperty.Get(e.HWnd);
                    if (!winp.NoActivate)
                    {
                        // 获取根窗口句柄，避免使用子窗口句柄
                        var parentHwnd = winp.GetRootWindow();
                        Update(parentHwnd);
                        ForegroundWindowChanged?.Invoke(this, new ForegroundWindowChangedEventArgs(parentHwnd, e.ChangeType));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in Hook_ForegroundWindowChanged");
                }
                break;
            default:
                return;
        }

    }

    public void Dispose()
    {
        hook.ForegroundWindowChanged -= Hook_ForegroundWindowChanged;
        hook.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Extension methods for IActiveWindowService
/// </summary>
public static class ActiveWindowServiceExtensions
{
    /// <summary>
    /// Ignore a WPF Window in the active window tracking
    /// </summary>
    /// <param name="service">The active window service</param>
    /// <param name="window">The WPF window to ignore</param>
    public static void Ignore(this IActiveWindowService service, Window window)
    {
        if (window == null) return;

        // Try to get handle immediately if window is already initialized
        var helper = new WindowInteropHelper(window);
        if (helper.Handle != IntPtr.Zero)
        {
            service.Ignore(helper.Handle);
            return;
        }

        // If no handle yet, wait for SourceInitialized event
        void OnSourceInitialized(object sender, EventArgs e)
        {
            window.SourceInitialized -= OnSourceInitialized;
            var handle = new WindowInteropHelper(window).Handle;
            if (handle != IntPtr.Zero)
            {
                service.Ignore(handle);
            }
        }

        window.SourceInitialized += OnSourceInitialized;
    }
}
