using Microsoft.SemanticKernel;

namespace QuickerExpressionAgent.Demo.Plugins;

/// <summary>
/// Simple time plugin for demonstration
/// </summary>
public class TimePlugin
{
    [KernelFunction]
    [System.ComponentModel.Description("Get the current date and time")]
    public string GetCurrentTime()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
    
    [KernelFunction]
    [System.ComponentModel.Description("Get the current date")]
    public string GetCurrentDate()
    {
        return DateTime.Now.ToString("yyyy-MM-dd");
    }
    
    [KernelFunction]
    [System.ComponentModel.Description("Get the day of the week")]
    public string GetDayOfWeek()
    {
        return DateTime.Now.DayOfWeek.ToString();
    }
}

