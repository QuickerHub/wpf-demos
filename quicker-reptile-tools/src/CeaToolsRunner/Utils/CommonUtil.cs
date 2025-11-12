using System;
using System.Diagnostics;

namespace Cea.Utils;

public static class CommonUtil
{
    public static void TryOpenFileOrUrl(string cmd, string args = "")
    {
        try
        {
            Process.Start(cmd, args);
        }
        catch (Exception e)
        {
            // Log error if needed
            Console.WriteLine($"Error opening file/URL: {e.Message}");
        }
    }
}

