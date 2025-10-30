using System;
using System.Threading;
using System.Threading.Tasks;

namespace WClipTools;

public static class Runner
{
    [STAThread]
    public static async Task<object?> WindowsClipAccess(string op, int count)
    {
        var api = new ClipApi();
        switch (op)
        {
            case "at":
                return await api.GetClipTextAt(count);
            case "take":
                return await api.GetClipTextAsync(count);
            case "clear":
                api.Clear(); break;
            case "clear_history":
                return api.ClearHistory();
            default:
                break;
        }
        return null;
    }
}