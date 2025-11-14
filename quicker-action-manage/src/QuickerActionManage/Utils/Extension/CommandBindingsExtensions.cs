using System;
using System.Windows.Input;
using System.Windows;

namespace QuickerActionManage.Utils.Extension
{
    /// <summary>
    /// CommandBindings extension methods
    /// </summary>
    public static class CommandBindingsExtensions
    {
        /// <summary>
        /// Add key gesture binding
        /// </summary>
        public static void AddKeyGesture(this System.Windows.Input.CommandBindingCollection bindings, KeyGesture gesture, ExecutedRoutedEventHandler handler)
        {
            bindings.Add(new CommandBinding(new RoutedCommand()
            {
                InputGestures = { gesture }
            }, handler));
        }
    }
}

