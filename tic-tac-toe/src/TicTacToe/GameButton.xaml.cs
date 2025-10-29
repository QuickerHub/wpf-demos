using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TicTacToe
{
    /// <summary>
    /// GameButton.xaml 的交互逻辑
    /// </summary>
    public partial class GameButton : Button, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private Piece piece = Piece.None;

        public GameButton()
        {
            InitializeComponent();
        }
        public Piece Piece
        {
            get => piece;
            set
            {
                piece = value;
                OnPropertyChanged(nameof(Piece));
            }
        }
    }
}
