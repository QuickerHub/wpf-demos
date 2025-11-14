using System;
using System.Diagnostics;
using System.Reflection;
using log4net;

namespace QuickerActionManage.Utils
{
    /// <summary>
    /// Common utility methods
    /// </summary>
    public static class CommonUtil
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CommonUtil));

        public static string GetAssemblyName() => Assembly.GetExecutingAssembly().GetName().Name;

        /// <summary>
        /// Try to open file or URL
        /// </summary>
        public static void TryOpenFileOrUrl(string cmd, string args = "")
        {
            try
            {
                Process.Start(cmd, args);
            }
            catch (Exception e)
            {
                _log.Error("Failed to open file or URL", e);
            }
        }

        /// <summary>
        /// Try to invoke action safely
        /// </summary>
        public static void TryInvoke(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch
            {
                // Ignore exceptions
            }
        }

        /// <summary>
        /// Try to invoke function safely
        /// </summary>
        public static T? TryInvoke<T>(Func<T> func) where T : class
        {
            try
            {
                return func?.Invoke();
            }
            catch
            {
                return null;
            }
        }
    }
}

