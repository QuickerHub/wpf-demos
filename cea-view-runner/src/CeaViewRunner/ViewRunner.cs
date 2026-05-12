using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CeaViewRunner.Infrastructure;
using CeaViewRunner.ViewModels;
using CeaViewRunner.Views;
using Newtonsoft.Json.Linq;
using Quicker.Public.Entities;
using Point = System.Drawing.Point;

namespace CeaViewRunner;

/// <summary>
/// Entry points for markdown message boxes, timer overlay, guides overlay, and custom window placement
/// (ported from CeaQuickerTools.ViewRunner).
/// </summary>
public static class ViewRunner
{
    private static readonly MultiValueDictionary<object, IntPtr> CustomWindowDict = new();

    private static readonly FileGlobalStateWriter StateWriter = new(typeof(ViewRunner).FullName ?? "CeaViewRunner.ViewRunner");

    static ViewRunner()
    {
        ViewRunnerResourceLoader.EnsureMerged();
    }

    public static T? ParseDictParam<T>(object? obj) where T : class
    {
        if (obj == null)
        {
            return null;
        }

        var jsonObject = JObject.FromObject(obj);
        foreach (var property in jsonObject.Properties().ToList())
        {
            if (property.Value.Type == JTokenType.Null
                || property.Value.Type == JTokenType.String && string.IsNullOrWhiteSpace(property.Value.ToString()))
            {
                property.Remove();
            }
        }

        return jsonObject.ToObject<T>();
    }

    public static IList<CommonOperationItem> GenerateItems(object? customButtons)
    {
        if (customButtons == null)
        {
            return new List<CommonOperationItem>();
        }

        if (customButtons is string strData)
        {
            return CommonOperationItem.ParseLines(strData, true, true);
        }

        if (customButtons is IList<string> listStrData)
        {
            return CommonOperationItem.ParseLines(listStrData);
        }

        return customButtons as IList<CommonOperationItem> ?? new List<CommonOperationItem>();
    }

    public static (bool isOk, bool doNotRemind) MessageBox3mdOkCancel(Window? owner, string markdown, bool showDoNotRemind)
    {
        ViewRunnerResourceLoader.EnsureMerged();
        var vm = new MessageBox3mdModel
        {
            MarkDown = markdown ?? "",
            CustomButtons = MessageBoxMdModel.OkCancelButtons,
            WindowParam = new(),
            ShowDoNotRemind = showDoNotRemind,
        };
        var win = new MessageBox3mdWindow
        {
            ViewModel = vm,
            DataContext = vm,
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        win.ShowDialog();
        return (isOk: vm.Result == "Ok", vm.DoNotRemind);
    }

    public static string MessageBox3md(CustomWindowParam cwp, string markdown, object? window = null, object? customButtons = null)
    {
        ViewRunnerResourceLoader.EnsureMerged();
        var vm = new MessageBox3mdModel
        {
            MarkDown = markdown ?? "",
            CustomButtons = GenerateItems(customButtons),
            WindowParam = ParseDictParam<MdWindowParamClass>(window),
        };
        var win = new MessageBox3mdWindow
        {
            ViewModel = vm,
            DataContext = vm,
        };
        ShowWindow(win, cwp);
        return vm.Result;
    }

    public static string MessageBox2md(
        CustomWindowParam cwp,
        string markdown,
        string title = "弹窗提示",
        object? window = null,
        object? customButtons = null)
    {
        ViewRunnerResourceLoader.EnsureMerged();
        var vm = new MessageBox2mdModel
        {
            MarkDown = markdown ?? "",
            CustomButtons = GenerateItems(customButtons),
            WindowParam = window == null
                ? new()
                : window.ToJson().TryJsonToObject<MdWindowParamClass>() ?? new(),
            Title = title,
        };
        var win = new MessageBox2mdWindow
        {
            ViewModel = vm,
            DataContext = vm,
        };
        ShowWindow(win, cwp);
        return vm.Result;
    }

    public static void ShowWindow(Window win) => ShowWindow(win, new());

    public static void ShowWindow(Window win, CustomWindowParam param)
    {
        ViewRunnerResourceLoader.EnsureMerged();
        if (param.IsEmptyTag)
        {
            param.WindowTag = win.GetType().FullName ?? win.GetType().Name;
        }

        if (param.Operation == CustomWindowParam.Operations.close)
        {
            foreach (IntPtr hwnd in CustomWindowDict.GetValues(param.WindowTag))
            {
                var w = ViewRunnerWindowHelper.GetWindow(hwnd);
                try
                {
                    w?.Close();
                }
                catch
                {
                    // ignore
                }
            }

            CustomWindowDict.RemoveKey(param.WindowTag);
            return;
        }

        win.SourceInitialized += (_, _) =>
        {
            var hwnd = win.GetHandle();
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            if (param.NoActive)
            {
                ViewRunnerWindowHelper.ApplyNoActivate(hwnd, true);
            }

            if (param.MousePenetration)
            {
                ViewRunnerWindowHelper.ApplyMousePenetration(hwnd, true);
            }

            if (param.Topmost)
            {
                win.Topmost = true;
                ViewRunnerWindowHelper.ApplyTopmostHwnd(hwnd, true);
            }

            if (param.CanUseQuicker)
            {
                QWindowHelperStub.SetCanUseQuicker(hwnd, true);
            }
        };

        win.ShowActivated = param.Activate;

        if (param.LoseFocusClose)
        {
            win.Deactivated += (_, _) => win.Close();
        }

        {
            var size = param.Size;
            if (size == null && param.LastSize)
            {
                var val = StateWriter.Read($"{param.WindowTag}.Size", "") as string;
                size = String2Point(val);
            }

            if (size != null)
            {
                var s1 = (Point)size;
                win.SourceInitialized += (_, _) => ViewRunnerWindowHelper.SetWindowSize(win.GetHandle(), s1.X, s1.Y);
            }
        }

        if (param.StartUpLocation == WindowLocations.NA)
        {
            var pos = param.Position;
            if (pos == null)
            {
                var val = StateWriter.Read($"{param.WindowTag}.Position", "") as string;
                pos = String2Point(val);
            }

            if (pos != null)
            {
                var p = (Point)pos;
                win.WindowStartupLocation = WindowStartupLocation.Manual;
                win.Left = 0;
                win.Top = -4000;
                win.ContentRendered += (_, _) =>
                    ViewRunnerWindowHelper.MoveWindowInToScreen(win.GetHandle(), p.X, p.Y);
            }
        }
        else
        {
            win.WindowStartupLocation = WindowStartupLocation.Manual;
            win.Left = 0;
            win.Top = -4000;
            win.ContentRendered += (_, _) => ViewRunnerWindowHelper.MoveWindow(win, param.StartUpLocation);
        }

        if (param.AutoCloseTime > 0)
        {
            win.DoActionOnLoaded(win.Close, (int)(param.AutoCloseTime * 1000));
        }

        win.DoActionOnLoaded(() => CustomWindowDict.Add(param.WindowTag, win.GetHandle()));
        win.Closing += (_, _) =>
        {
            var hwnd = win.GetHandle();
            if (hwnd != IntPtr.Zero && NativeWin32.GetWindowRect(hwnd, out var rect))
            {
                param.Position = new Point(rect.Left, rect.Top);
                StateWriter.Write($"{param.WindowTag}.Position", Point2String(param.Position));
                param.Size = new Point(rect.Width, rect.Height);
                StateWriter.Write($"{param.WindowTag}.Size", Point2String(param.Size));
            }
        };

        if (param.Operation == CustomWindowParam.Operations.wait)
        {
            ViewRunnerWindowHelper.ShowWindowAndWaitClose(win, param.Activate);
        }
        else if (param.Operation == CustomWindowParam.Operations.show)
        {
            ViewRunnerWindowHelper.ShowWindow(win, param.Activate);
        }
    }

    public static bool TimeWindow(
        CustomWindowParam param,
        object? skinObj = null,
        string type = "now",
        string duration = "00:01:00",
        DateTime? timeEnd = null,
        string? tips = null,
        Action? timeOut = null,
        bool useLauner = false)
    {
        ViewRunnerResourceLoader.EnsureMerged();
        var skin = skinObj?.ToJson().TryJsonToObject<TimeWindowViewModel.TimeWindowSkin>() ?? new();
        var vm = new TimeWindowViewModel
        {
            Skin = skin,
            Tooltip = tips,
        };

        if (timeEnd == null || timeEnd <= DateTime.Now)
        {
            var ts = duration.Split(':').Select(x => Convert.ToInt32(x)).ToList();
            var time = ts.Count >= 3
                ? new TimeSpan(ts[0], ts[1], ts[2])
                : new TimeSpan(ts[0], ts[1], 0);
            timeEnd = DateTime.Now + time;
        }

        var win = TimeWindowRunner.CreateWindow(vm, type, timeEnd, tips, timeOut, useLauner);

        ShowWindow(win, param);
        return vm.Success;
    }

    public static void ShowGuidesWindow()
    {
        ViewRunnerResourceLoader.EnsureMerged();
        var win = new GuidesWindow();
        var param = new CustomWindowParam
        {
            NoActive = true,
            MousePenetration = true,
            StartUpLocation = WindowLocations.CenterScreen,
        };
        ShowWindow(win, param);
    }

    private static string Point2String(Point? p) => p == null ? "" : $"{p.Value.X},{p.Value.Y}";

    private static Point? String2Point(string? postr)
    {
        if (postr == null)
        {
            return null;
        }

        var pos = postr.Split(',').Select(x =>
        {
            int.TryParse(x, out var val);
            return val;
        }).ToArray();
        return pos.Length switch
        {
            >= 2 => new Point(pos[0], pos[1]),
            _ => null,
        };
    }
}
