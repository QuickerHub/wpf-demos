using System;
using System.Collections.Generic;
using System.Linq;
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
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private TicTacToeAI _ai = null!;
        private Piece[,] _board = new Piece[3, 3];
        private bool _gameEnded = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            // 初始化AI，默认困难难度
            _ai = new TicTacToeAI(TicTacToeAI.Difficulty.Hard);
            _gameEnded = false;
            UpdateGameStatus("游戏开始！你是 X，AI 是 O");
        }

        public Piece[,] Pieces = new Piece[3,3];
        private void ClearPieces()
        {
            var buttons = GetButtons();
            foreach(var btn in buttons)
            {
                btn.Piece = Piece.None;
            }
            
            // 清空内部棋盘状态
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    _board[i, j] = Piece.None;
                }
            }
            
            _gameEnded = false;
        }
        private List<GameButton> GetButtons()
        {
            var child = BtnGird.Children.Cast<UIElement>().Where(x => x.GetType() == typeof(GameButton)).Select(x => x as GameButton).Where(x => x != null).ToList();
            return child!;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_gameEnded) return;

            if (e.Source is GameButton btn && btn.Piece == Piece.None)
            {
                // 玩家下棋
                btn.Piece = Piece.Times;
                int tag = Convert.ToInt32(btn.Tag);
                int row = tag / 3;
                int col = tag % 3;
                _board[row, col] = Piece.Times;

                // 检查游戏是否结束
                if (CheckGameEnd())
                    return;

                // AI下棋
                MakeAIMove();
            }
        }

        private void MakeAIMove()
        {
            if (_gameEnded) return;

            // 获取当前难度设置
            var selectedItem = DifficultyComboBox.SelectedItem as ComboBoxItem;
            var difficultyTag = selectedItem?.Tag?.ToString() ?? "Hard";
            var difficulty = (TicTacToeAI.Difficulty)Enum.Parse(typeof(TicTacToeAI.Difficulty), difficultyTag);
            
            // 更新AI难度
            _ai = new TicTacToeAI(difficulty);

            // 获取AI的最佳移动
            var (row, col) = _ai.GetBestMove(_board, Piece.Circle, Piece.Times);
            
            if (row >= 0 && col >= 0 && _board[row, col] == Piece.None)
            {
                // 找到对应的按钮并下棋
                var buttons = GetButtons();
                var aiButton = buttons.FirstOrDefault(b => 
                {
                    int tag = Convert.ToInt32(b.Tag);
                    return tag / 3 == row && tag % 3 == col;
                });

                if (aiButton != null)
                {
                    aiButton.Piece = Piece.Circle;
                    _board[row, col] = Piece.Circle;
                }
            }

            // 检查游戏是否结束
            CheckGameEnd();
        }

        private bool CheckGameEnd()
        {
            var winner = CheckWinner();
            if (winner != Piece.None)
            {
                _gameEnded = true;
                switch (winner)
                {
                    case Piece.Times:
                        MessageBtn.Text = "你赢了!";
                        UpdateGameStatus("恭喜！你战胜了AI！");
                        break;
                    case Piece.Circle:
                        MessageBtn.Text = "你输了!";
                        UpdateGameStatus("AI获胜！再试一次吧！");
                        break;
                }
                return true;
            }

            // 检查平局
            if (IsBoardFull())
            {
                _gameEnded = true;
                MessageBtn.Text = "平局";
                UpdateGameStatus("平局！势均力敌！");
                return true;
            }

            return false;
        }

        private Piece CheckWinner()
        {
            // 检查行
            for (int i = 0; i < 3; i++)
            {
                if (_board[i, 0] != Piece.None && _board[i, 0] == _board[i, 1] && _board[i, 1] == _board[i, 2])
                    return _board[i, 0];
            }

            // 检查列
            for (int j = 0; j < 3; j++)
            {
                if (_board[0, j] != Piece.None && _board[0, j] == _board[1, j] && _board[1, j] == _board[2, j])
                    return _board[0, j];
            }

            // 检查对角线
            if (_board[0, 0] != Piece.None && _board[0, 0] == _board[1, 1] && _board[1, 1] == _board[2, 2])
                return _board[0, 0];

            if (_board[0, 2] != Piece.None && _board[0, 2] == _board[1, 1] && _board[1, 1] == _board[2, 0])
                return _board[0, 2];

            return Piece.None;
        }

        private bool IsBoardFull()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (_board[i, j] == Piece.None)
                        return false;
                }
            }
            return true;
        }

        private void UpdateGameStatus(string message)
        {
            if (GameStatusText != null)
            {
                GameStatusText.Text = message;
            }
        }

        private void ReSet_Click(object sender, RoutedEventArgs e)
        {
            MessageBtn.Text = "";
            ClearPieces();
            UpdateGameStatus("游戏重置！选择难度开始新游戏");
        }
    }
}
