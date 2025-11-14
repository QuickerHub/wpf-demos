using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using log4net;
using Quicker.Settings.Pages.About;
using Quicker.View;

namespace QuickerActionManage.Utils
{
    /// <summary>
    /// Action static information helper
    /// </summary>
    public class ActionStaticInfo
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(ActionStaticInfo));
        private readonly UsageStatisticsInfoPage _page;
        
        public UsageStatisticsInfoPage Page => _page;
        
        public ActionStaticInfo()
        {
            _page = new UsageStatisticsInfoPage();
            TriggerLoadedEvent(_page);
        }

        public System.Collections.Generic.List<ActionCountItem> GetActionCountItems()
        {
            var listview = GetField<ListView>(_page, "LvActions")!;
            return listview.Items.Cast<ActionCountItem>().ToList();
        }

        /// <summary>
        /// Trigger the Loaded event for a WPF control
        /// </summary>
        /// <param name="control">The control to trigger Loaded event for</param>
        public static void TriggerLoadedEvent(FrameworkElement control)
        {
            if (control == null) return;

            // Create a temporary window to ensure the control is in the visual tree
            var tempWindow = new Window
            {
                Width = 0,
                Height = 0,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Content = control
            };
            tempWindow.Show();
            tempWindow.Content = null; // Detach the control from the window
            tempWindow.Close();
        }

        public static T? GetField<T>(object sender, string name) where T : class
        {
            var tp = sender.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return tp?.GetValue(sender) as T;
        }
    }
}

