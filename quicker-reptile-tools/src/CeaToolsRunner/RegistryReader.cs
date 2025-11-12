using Microsoft.Win32;


public class RegistryReader
{
    public void GetInstalledSoftwareInfo()
    {
        // 获取 64 位 Windows 注册表中的安装信息
        RegistryKey key64 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
        if (key64 != null)
        {
            PrintInstalledSoftware(key64);
        }

        // 获取 32 位 Windows 注册表中的安装信息
        RegistryKey key32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
        if (key32 != null)
        {
            PrintInstalledSoftware(key32);
        }
    }

    private void PrintInstalledSoftware(RegistryKey key)
    {
        foreach (string subKeyName in key.GetSubKeyNames())
        {
            RegistryKey subKey = key.OpenSubKey(subKeyName);
            if (subKey != null)
            {
                string displayName = (string)subKey.GetValue("DisplayName");
                string installLocation = (string)subKey.GetValue("InstallLocation");

                if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(installLocation))
                {
                    Console.WriteLine($"软件名：{displayName}");
                    Console.WriteLine($"安装路径：{installLocation}");
                    Console.WriteLine();
                }

                subKey.Close();
            }
        }
    }
}
