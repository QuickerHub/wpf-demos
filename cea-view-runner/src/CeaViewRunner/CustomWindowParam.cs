using System;
using System.ComponentModel;
using System.Drawing;
using CeaViewRunner.Infrastructure;

namespace CeaViewRunner;

public class CustomWindowParam
{
    public CustomWindowParam()
    {
    }

    public CustomWindowParam(
        string operation,
        string windowTag = "",
        bool lostFocusClose = false,
        double autoCloseTime = 0,
        string activeMode = "",
        string showLoc = "",
        bool topmost = false)
    {
        SetOperation(operation);
        WindowTag = windowTag;
        LoseFocusClose = lostFocusClose;
        AutoCloseTime = autoCloseTime;
        SetActivationMode(activeMode);
        SetStartUpLocation(showLoc);
        Topmost = topmost;
    }

    public enum ActivationModes
    {
        NotActivatable,
        NotActivatableMouseThrough,
        NotActivated,
        AutoActivate,
    }

    private void SetActivationMode(string value)
    {
        if (!Enum.TryParse(value, out ActivationModes mode))
        {
            mode = ActivationModes.AutoActivate;
        }

        switch (mode)
        {
            case ActivationModes.NotActivatable:
                NoActive = true;
                break;
            case ActivationModes.NotActivatableMouseThrough:
                NoActive = true;
                MousePenetration = true;
                break;
            case ActivationModes.NotActivated:
                break;
            case ActivationModes.AutoActivate:
                Activate = true;
                break;
            default:
                break;
        }
    }

    [DisplayName("激活")]
    public bool Activate { get; set; }

    [DisplayName("无焦点模式")]
    public bool NoActive { get; set; }

    [DisplayName("鼠标穿透")]
    public bool MousePenetration { get; set; }

    [DisplayName("窗口标识")]
    public object WindowTag { get; set; } = "";

    public bool IsEmptyTag => WindowTag == null || WindowTag as string == "";

    [DisplayName("启动位置")]
    public WindowLocations StartUpLocation { get; set; }

    [DisplayName("自定义位置")]
    public Point? Position { get; set; }

    [DisplayName("是用上次窗口大小")]
    public bool LastSize { get; set; }

    [DisplayName("窗口大小")]
    public Point? Size { get; set; }

    private void SetStartUpLocation(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (Enum.TryParse(value, true, out WindowLocations loc))
        {
            StartUpLocation = loc;
        }
        else
        {
            StartUpLocation = value switch
            {
                "WithMouse1" => WindowLocations.MouseAround,
                "WithMouse2" => WindowLocations.MouseRightBottom,
                _ => WindowLocations.CenterScreen,
            };
        }
    }

    [DisplayName("失去焦点后关闭")]
    public bool LoseFocusClose { get; set; }

    [DisplayName("自动关闭时间（秒）")]
    public double AutoCloseTime { get; set; }

    [DisplayName("窗口置顶")]
    public bool Topmost { get; set; }

    [DisplayName("支持窗口上触发quicker")]
    public bool CanUseQuicker { get; set; }

    public enum Operations
    {
        show,
        wait,
        close,
    }

    public void SetOperation(string op)
    {
        switch (op)
        {
            case "show":
                Operation = Operations.show;
                break;
            case "wait":
            case "show_wait":
                Operation = Operations.wait;
                break;
            case "close":
                Operation = Operations.close;
                break;
            default:
                break;
        }
    }

    [DisplayName("操作方式")]
    public Operations Operation { get; set; }
}
