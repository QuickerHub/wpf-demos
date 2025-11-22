using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DependencyPropertyGenerator;

namespace QuickerExpressionAgent.Desktop.Behaviors;

/// <summary>
/// Attached behavior for handling Enter key command on TextBox
/// Shift+Enter allows newline, Enter without Shift triggers the command
/// </summary>
[AttachedDependencyProperty<ICommand, Control>("EnterCommand")]
public static partial class EnterCommandBehavior
{
    static partial  void OnEnterCommandChanged(Control control, ICommand? oldValue, ICommand? newValue)
    {
        if (oldValue != null)
        {
            control.PreviewKeyDown -= Control_PreviewKeyDown;
        }

        if (newValue != null)
        {
            control.PreviewKeyDown += Control_PreviewKeyDown;
        }
    }

    private static void Control_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
        {
            var control = (Control)sender;
            var command = GetEnterCommand(control);
            
            if (command?.CanExecute(null) == true)
            {
                e.Handled = true; // Prevent default newline behavior
                command.Execute(null);
            }
        }
        // Shift+Enter: allow default behavior (new line) - don't handle
    }
}

