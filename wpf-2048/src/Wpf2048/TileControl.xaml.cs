using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Wpf2048
{
    /// <summary>
    /// TileControl.xaml 的交互逻辑
    /// </summary>
    public partial class TileControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(int), typeof(TileControl),
                new PropertyMetadata(0, OnValueChanged));

        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private Brush _backgroundColor = new SolidColorBrush(Color.FromRgb(0xEE, 0xE4, 0xDA));
        private Brush _textColor = new SolidColorBrush(Color.FromRgb(0x77, 0x6E, 0x65));

        public Brush BackgroundColor
        {
            get => _backgroundColor;
            private set
            {
                _backgroundColor = value;
                OnPropertyChanged();
            }
        }

        public Brush TextColor
        {
            get => _textColor;
            private set
            {
                _textColor = value;
                OnPropertyChanged();
            }
        }

        public TileControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TileControl control)
            {
                control.UpdateColors();
            }
        }

        private void UpdateColors()
        {
            int value = Value;
            
            // Background colors based on value
            BackgroundColor = value switch
            {
                0 => new SolidColorBrush(Color.FromRgb(0xEE, 0xE4, 0xDA)),
                2 => new SolidColorBrush(Color.FromRgb(0xEE, 0xE4, 0xDA)),
                4 => new SolidColorBrush(Color.FromRgb(0xED, 0xE0, 0xC8)),
                8 => new SolidColorBrush(Color.FromRgb(0xF2, 0xB1, 0x79)),
                16 => new SolidColorBrush(Color.FromRgb(0xF5, 0x95, 0x63)),
                32 => new SolidColorBrush(Color.FromRgb(0xF6, 0x7C, 0x5F)),
                64 => new SolidColorBrush(Color.FromRgb(0xF6, 0x5E, 0x3B)),
                128 => new SolidColorBrush(Color.FromRgb(0xED, 0xCF, 0x72)),
                256 => new SolidColorBrush(Color.FromRgb(0xED, 0xCC, 0x61)),
                512 => new SolidColorBrush(Color.FromRgb(0xED, 0xC8, 0x50)),
                1024 => new SolidColorBrush(Color.FromRgb(0xED, 0xC5, 0x3F)),
                2048 => new SolidColorBrush(Color.FromRgb(0xED, 0xC2, 0x2E)),
                _ => new SolidColorBrush(Color.FromRgb(0x3C, 0x3A, 0x32))
            };

            // Text colors
            TextColor = value <= 4 
                ? new SolidColorBrush(Color.FromRgb(0x77, 0x6E, 0x65))
                : new SolidColorBrush(Colors.White);

            // Update font size based on value
            if (TileText != null)
            {
                TileText.FontSize = value switch
                {
                    >= 10000 => 20,
                    >= 1000 => 24,
                    >= 100 => 28,
                    _ => 32
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

