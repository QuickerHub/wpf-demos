using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading.Tasks;

namespace WpfGobang
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int BOARD_SIZE = 15;
        private const int CELL_SIZE = 40; // 每个格子的大小
        private const int BOARD_OFFSET = 20; // 棋盘边距
        private const int PIECE_RADIUS = 18; // 棋子半径

        private Board _board;
        private GobangAI _ai;
        private bool _gameEnded;
        private bool _isPlayerTurn = true; // true表示玩家回合，false表示AI回合
        private Piece _playerPiece = Piece.Black; // 玩家使用黑子
        private Piece _aiPiece = Piece.White; // AI使用白子
        private int _lastMoveRow = -1; // 最后落子行
        private int _lastMoveCol = -1; // 最后落子列
        private Ellipse _lastMoveMarker; // 最后落子标记

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
            DrawBoard();
        }

        private void InitializeGame()
        {
            _board = new Board();
            _gameEnded = false;
            _isPlayerTurn = true;
            _lastMoveRow = -1;
            _lastMoveCol = -1;
            _lastMoveMarker = null;
            
            var selectedItem = DifficultyComboBox.SelectedItem as ComboBoxItem;
            var difficultyTag = selectedItem?.Tag?.ToString() ?? "Medium";
            var difficulty = (GobangAI.Difficulty)Enum.Parse(typeof(GobangAI.Difficulty), difficultyTag);
            _ai = new GobangAI(difficulty);
            
            UpdateStatus("游戏开始！黑子先行");
            UpdateGameStatus("轮到你了");
        }

        private void DrawBoard()
        {
            BoardCanvas.Children.Clear();
            _lastMoveMarker = null;
            
            // 绘制棋盘网格线
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                // 横线
                var hLine = new Line
                {
                    X1 = BOARD_OFFSET,
                    Y1 = BOARD_OFFSET + i * CELL_SIZE,
                    X2 = BOARD_OFFSET + (BOARD_SIZE - 1) * CELL_SIZE,
                    Y2 = BOARD_OFFSET + i * CELL_SIZE,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                BoardCanvas.Children.Add(hLine);
                
                // 竖线
                var vLine = new Line
                {
                    X1 = BOARD_OFFSET + i * CELL_SIZE,
                    Y1 = BOARD_OFFSET,
                    X2 = BOARD_OFFSET + i * CELL_SIZE,
                    Y2 = BOARD_OFFSET + (BOARD_SIZE - 1) * CELL_SIZE,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                BoardCanvas.Children.Add(vLine);
            }
            
            // 绘制天元和星位
            int[] starPositions = { 3, 7, 11 };
            foreach (int row in starPositions)
            {
                foreach (int col in starPositions)
                {
                    var star = new Ellipse
                    {
                        Width = 8,
                        Height = 8,
                        Fill = Brushes.Black
                    };
                    Canvas.SetLeft(star, BOARD_OFFSET + col * CELL_SIZE - 4);
                    Canvas.SetTop(star, BOARD_OFFSET + row * CELL_SIZE - 4);
                    BoardCanvas.Children.Add(star);
                }
            }
            
            // 重新绘制所有棋子
            RedrawPieces();
        }

        private void RedrawPieces()
        {
            // 移除所有棋子和标记（保留网格线）
            var piecesToRemove = new System.Collections.Generic.List<UIElement>();
            foreach (UIElement child in BoardCanvas.Children)
            {
                if (child is Ellipse ellipse && ellipse.Tag != null)
                {
                    piecesToRemove.Add(child);
                }
            }
            foreach (var piece in piecesToRemove)
            {
                BoardCanvas.Children.Remove(piece);
            }
            
            _lastMoveMarker = null;
            int savedLastRow = _lastMoveRow;
            int savedLastCol = _lastMoveCol;
            _lastMoveRow = -1;
            _lastMoveCol = -1;
            
            // 绘制所有棋子
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    Piece piece = _board.GetPiece(i, j);
                    if (piece != Piece.None)
                    {
                        bool isLastMove = (i == savedLastRow && j == savedLastCol);
                        DrawPiece(i, j, piece, isLastMove);
                    }
                }
            }
        }

        private void DrawPiece(int row, int col, Piece piece, bool isLastMove = false)
        {
            var ellipse = new Ellipse
            {
                Width = PIECE_RADIUS * 2,
                Height = PIECE_RADIUS * 2,
                Fill = piece == Piece.Black ? Brushes.Black : Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Tag = "piece" // 标记为棋子
            };
            
            double x = BOARD_OFFSET + col * CELL_SIZE - PIECE_RADIUS;
            double y = BOARD_OFFSET + row * CELL_SIZE - PIECE_RADIUS;
            
            Canvas.SetLeft(ellipse, x);
            Canvas.SetTop(ellipse, y);
            
            BoardCanvas.Children.Add(ellipse);

            // 如果是最后落子位置，添加特殊标记
            if (isLastMove)
            {
                UpdateLastMoveMarker(row, col);
            }
        }

        /// <summary>
        /// 更新最后落子位置的标记（带动画效果）
        /// </summary>
        private void UpdateLastMoveMarker(int row, int col)
        {
            // 移除旧的标记
            if (_lastMoveMarker != null)
            {
                BoardCanvas.Children.Remove(_lastMoveMarker);
            }

            // 创建新的标记（高亮边框）
            _lastMoveMarker = new Ellipse
            {
                Width = PIECE_RADIUS * 2 + 8, // 比棋子大一点
                Height = PIECE_RADIUS * 2 + 8,
                Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 215, 0)), // 金黄色
                StrokeThickness = 3,
                Fill = Brushes.Transparent,
                Tag = "lastMoveMarker"
            };

            double x = BOARD_OFFSET + col * CELL_SIZE - PIECE_RADIUS - 4;
            double y = BOARD_OFFSET + row * CELL_SIZE - PIECE_RADIUS - 4;

            Canvas.SetLeft(_lastMoveMarker, x);
            Canvas.SetTop(_lastMoveMarker, y);
            Canvas.SetZIndex(_lastMoveMarker, 1); // 确保标记在棋子之上

            BoardCanvas.Children.Add(_lastMoveMarker);

            // 添加脉冲动画效果
            var animation = new DoubleAnimation
            {
                From = 0.3,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.6),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            _lastMoveMarker.Opacity = 0.3;
            _lastMoveMarker.BeginAnimation(UIElement.OpacityProperty, animation);

            // 保存最后落子位置
            _lastMoveRow = row;
            _lastMoveCol = col;
        }

        private void BoardCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_gameEnded || !_isPlayerTurn)
                return;

            Point position = e.GetPosition(BoardCanvas);
            
            // 计算点击位置对应的行列
            int col = (int)Math.Round((position.X - BOARD_OFFSET) / CELL_SIZE);
            int row = (int)Math.Round((position.Y - BOARD_OFFSET) / CELL_SIZE);
            
            // 检查位置是否有效
            if (row >= 0 && row < BOARD_SIZE && col >= 0 && col < BOARD_SIZE)
            {
                if (_board.PlacePiece(row, col, _playerPiece))
                {
                    DrawPiece(row, col, _playerPiece, isLastMove: true);
                    _isPlayerTurn = false;
                    
                    // 检查游戏是否结束
                    if (CheckGameEnd())
                        return;
                    
                    UpdateStatus("AI思考中...");
                    UpdateGameStatus("AI思考中...");
                    
                    // AI下棋（异步执行，避免UI卡顿）
                    Task.Run(() => MakeAIMove());
                }
            }
        }

        private void MakeAIMove()
        {
            // 在后台线程计算AI的移动
            var (row, col) = _ai.GetBestMove(_board, _aiPiece, _playerPiece);
            
            // 回到UI线程更新界面
            Dispatcher.Invoke(() =>
            {
                if (row >= 0 && col >= 0 && _board.PlacePiece(row, col, _aiPiece))
                {
                    DrawPiece(row, col, _aiPiece, isLastMove: true);
                    _isPlayerTurn = true;
                    
                    // 检查游戏是否结束
                    if (!CheckGameEnd())
                    {
                        UpdateStatus("轮到你了");
                        UpdateGameStatus("轮到你了");
                    }
                }
            });
        }

        private bool CheckGameEnd()
        {
            var winner = _board.CheckWinner();
            if (winner != Piece.None)
            {
                _gameEnded = true;
                _isPlayerTurn = false;
                
                if (winner == _playerPiece)
                {
                    UpdateStatus("恭喜！你赢了！");
                    UpdateGameStatus("恭喜！你战胜了AI！");
                }
                else
                {
                    UpdateStatus("AI获胜！");
                    UpdateGameStatus("AI获胜！再试一次吧！");
                }
                return true;
            }
            
            if (_board.IsFull())
            {
                _gameEnded = true;
                _isPlayerTurn = false;
                UpdateStatus("平局！");
                UpdateGameStatus("平局！势均力敌！");
                return true;
            }
            
            return false;
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        private void UpdateGameStatus(string message)
        {
            GameStatusText.Text = message;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            InitializeGame();
            DrawBoard();
        }

        private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ai != null)
            {
                var selectedItem = DifficultyComboBox.SelectedItem as ComboBoxItem;
                var difficultyTag = selectedItem?.Tag?.ToString() ?? "Medium";
                var difficulty = (GobangAI.Difficulty)Enum.Parse(typeof(GobangAI.Difficulty), difficultyTag);
                _ai = new GobangAI(difficulty);
            }
        }
    }
}

