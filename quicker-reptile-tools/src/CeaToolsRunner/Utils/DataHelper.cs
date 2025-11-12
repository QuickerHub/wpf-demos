using System;
using System.IO;

namespace Cea.Utils;

public static class DataHelper
{
    public static string GetDataFolder(string key)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Quicker", key);
    }
}

