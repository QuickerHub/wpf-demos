using System;
using System.Reflection;
using System.Windows;
using QuickerActionPanel.Utils;

namespace QuickerActionPanel
{
    /// <summary>
    /// Runner for Quicker integration
    /// </summary>
    public static class Runner
    {
        static Runner()
        {
            Loader.LoadThemeXamls(typeof(Runner).Assembly, "Theme.xaml");
        }

        /// <summary>
        /// Example method for Quicker integration
        /// </summary>
        public static void Run()
        {

            // TODO: Implement your logic here
        }
    }
}

