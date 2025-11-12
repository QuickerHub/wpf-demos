using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Wpf2048
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Game2048 _game;
        private TileControl[,] _tiles;
        private int[,] _previousGrid;
        private Game2048.Direction? _lastDirection;

        public MainWindow()
        {
            InitializeComponent();
            _game = new Game2048();
            _tiles = new TileControl[4, 4];
            _previousGrid = new int[4, 4];
            
            InitializeTiles();
            UpdateDisplay();
            
            _game.ScoreChanged += Game_ScoreChanged;
            _game.GameOver += Game_GameOver;
            _game.GameWon += Game_GameWon;
        }

        private void InitializeTiles()
        {
            const double tileSize = 90; // 400 - 10 (padding) / 4 - 5 (spacing) ≈ 90
            const double spacing = 5;
            
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    var tile = new TileControl();
                    tile.Width = tileSize;
                    tile.Height = tileSize;
                    
                    // Position in Canvas
                    Canvas.SetLeft(tile, j * (tileSize + spacing) + spacing);
                    Canvas.SetTop(tile, i * (tileSize + spacing) + spacing);
                    
                    GameGrid.Children.Add(tile);
                    _tiles[i, j] = tile;
                    
                    // Set initial transform
                    var transformGroup = new TransformGroup();
                    transformGroup.Children.Add(new TranslateTransform());
                    transformGroup.Children.Add(new ScaleTransform());
                    tile.RenderTransform = transformGroup;
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (_game.IsGameOver && e.Key != Key.R)
            {
                return;
            }

            Game2048.Direction? direction = e.Key switch
            {
                Key.Up => Game2048.Direction.Up,
                Key.Down => Game2048.Direction.Down,
                Key.Left => Game2048.Direction.Left,
                Key.Right => Game2048.Direction.Right,
                _ => null
            };

            if (direction.HasValue)
            {
                // Save previous grid state
                var currentGrid = _game.Grid;
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        _previousGrid[i, j] = currentGrid[i, j];
                    }
                }
                
                _lastDirection = direction.Value;
                _game.Move(direction.Value);
                UpdateDisplayWithAnimation();
            }
            else if (e.Key == Key.R)
            {
                NewGame();
            }
        }

        private void UpdateDisplay()
        {
            var grid = _game.Grid;
            
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    _tiles[i, j].Value = grid[i, j];
                }
            }

            ScoreText.Text = _game.Score.ToString();
            BestScoreText.Text = _game.BestScore.ToString();
        }

        private void UpdateDisplayWithAnimation()
        {
            var grid = _game.Grid;
            const double tileSize = 90;
            const double spacing = 5;
            
            // Track which tiles moved and which are new
            var movements = new Dictionary<(int fromRow, int fromCol), (int toRow, int toCol)>();
            var newTiles = new List<(int row, int col)>();
            var usedFromPositions = new HashSet<(int row, int col)>();
            
            if (!_lastDirection.HasValue)
            {
                // No direction, just update display
                UpdateDisplay();
                return;
            }
            
            // Track movements based on direction
            switch (_lastDirection.Value)
            {
                case Game2048.Direction.Left:
                    TrackMovementsLeft(grid, movements, newTiles, usedFromPositions);
                    break;
                case Game2048.Direction.Right:
                    TrackMovementsRight(grid, movements, newTiles, usedFromPositions);
                    break;
                case Game2048.Direction.Up:
                    TrackMovementsUp(grid, movements, newTiles, usedFromPositions);
                    break;
                case Game2048.Direction.Down:
                    TrackMovementsDown(grid, movements, newTiles, usedFromPositions);
                    break;
            }
            
            // Animate movements
            foreach (var movement in movements)
            {
                var fromRow = movement.Key.fromRow;
                var fromCol = movement.Key.fromCol;
                var toRow = movement.Value.toRow;
                var toCol = movement.Value.toCol;
                
                var tile = _tiles[fromRow, fromCol];
                var transformGroup = tile.RenderTransform as TransformGroup;
                if (transformGroup != null)
                {
                    var translateTransform = transformGroup.Children[0] as TranslateTransform;
                    if (translateTransform != null)
                    {
                        var fromX = fromCol * (tileSize + spacing) + spacing;
                        var fromY = fromRow * (tileSize + spacing) + spacing;
                        var toX = toCol * (tileSize + spacing) + spacing;
                        var toY = toRow * (tileSize + spacing) + spacing;
                        
                        var deltaX = toX - fromX;
                        var deltaY = toY - fromY;
                        
                        // Ensure tile is at starting position
                        Canvas.SetLeft(tile, fromX);
                        Canvas.SetTop(tile, fromY);
                        
                        // Animate movement
                        translateTransform.X = 0;
                        translateTransform.Y = 0;
                        
                        var animX = new DoubleAnimation(0, deltaX, TimeSpan.FromMilliseconds(150));
                        var animY = new DoubleAnimation(0, deltaY, TimeSpan.FromMilliseconds(150));
                        
                        translateTransform.BeginAnimation(TranslateTransform.XProperty, animX);
                        translateTransform.BeginAnimation(TranslateTransform.YProperty, animY);
                        
                        // Update Canvas position after animation completes
                        animX.Completed += (s, e) =>
                        {
                            Canvas.SetLeft(tile, toX);
                            Canvas.SetTop(tile, toY);
                            translateTransform.X = 0;
                            translateTransform.Y = 0;
                        };
                    }
                }
            }
            
            // Update all tile values and positions
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    var tile = _tiles[i, j];
                    var x = j * (tileSize + spacing) + spacing;
                    var y = i * (tileSize + spacing) + spacing;
                    
                    // Update value
                    tile.Value = grid[i, j];
                    
                    // Update position for tiles that didn't move
                    if (!movements.ContainsKey((i, j)))
                    {
                        Canvas.SetLeft(tile, x);
                        Canvas.SetTop(tile, y);
                        
                        var transformGroup = tile.RenderTransform as TransformGroup;
                        if (transformGroup != null)
                        {
                            var translateTransform = transformGroup.Children[0] as TranslateTransform;
                            if (translateTransform != null)
                            {
                                translateTransform.X = 0;
                                translateTransform.Y = 0;
                            }
                        }
                    }
                }
            }
            
            // Animate new tiles with scale
            foreach (var (row, col) in newTiles)
            {
                var tile = _tiles[row, col];
                var transformGroup = tile.RenderTransform as TransformGroup;
                if (transformGroup != null)
                {
                    var scaleTransform = transformGroup.Children[1] as ScaleTransform;
                    if (scaleTransform != null)
                    {
                        scaleTransform.ScaleX = 0;
                        scaleTransform.ScaleY = 0;
                        
                        var animX = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(100));
                        var animY = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(100));
                        
                        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
                        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
                    }
                }
            }

            ScoreText.Text = _game.Score.ToString();
            BestScoreText.Text = _game.BestScore.ToString();
        }

        private void TrackMovementsLeft(int[,] grid, Dictionary<(int, int), (int, int)> movements, 
            List<(int, int)> newTiles, HashSet<(int, int)> usedFromPositions)
        {
            // For each row, track movements from right to left
            for (int row = 0; row < 4; row++)
            {
                // Get non-zero tiles from previous grid in this row (right to left)
                var prevTiles = new List<(int col, int value)>();
                for (int col = 0; col < 4; col++)
                {
                    if (_previousGrid[row, col] != 0)
                    {
                        prevTiles.Add((col, _previousGrid[row, col]));
                    }
                }
                
                // Get non-zero tiles from new grid in this row (left to right)
                var newTilesInRow = new List<(int col, int value)>();
                for (int col = 0; col < 4; col++)
                {
                    if (grid[row, col] != 0)
                    {
                        newTilesInRow.Add((col, grid[row, col]));
                    }
                }
                
                // Match tiles: first prev tile goes to first new position, etc.
                int prevIdx = 0;
                int newIdx = 0;
                
                while (prevIdx < prevTiles.Count && newIdx < newTilesInRow.Count)
                {
                    var prevTile = prevTiles[prevIdx];
                    var newTile = newTilesInRow[newIdx];
                    
                    if (prevTile.value == newTile.value)
                    {
                        // Same tile, just moved
                        if (prevTile.col != newTile.col)
                        {
                            movements[(row, prevTile.col)] = (row, newTile.col);
                            usedFromPositions.Add((row, prevTile.col));
                        }
                        prevIdx++;
                        newIdx++;
                    }
                    else if (prevTile.value * 2 == newTile.value && prevIdx + 1 < prevTiles.Count && 
                             prevTiles[prevIdx + 1].value == prevTile.value)
                    {
                        // Merged: two tiles merged into one
                        movements[(row, prevTile.col)] = (row, newTile.col);
                        movements[(row, prevTiles[prevIdx + 1].col)] = (row, newTile.col);
                        usedFromPositions.Add((row, prevTile.col));
                        usedFromPositions.Add((row, prevTiles[prevIdx + 1].col));
                        prevIdx += 2;
                        newIdx++;
                    }
                    else
                    {
                        // New tile appeared
                        newTiles.Add((row, newTile.col));
                        newIdx++;
                    }
                }
                
                // Remaining new tiles are new
                while (newIdx < newTilesInRow.Count)
                {
                    newTiles.Add((row, newTilesInRow[newIdx].col));
                    newIdx++;
                }
            }
        }

        private void TrackMovementsRight(int[,] grid, Dictionary<(int, int), (int, int)> movements, 
            List<(int, int)> newTiles, HashSet<(int, int)> usedFromPositions)
        {
            // For each row, track movements from left to right
            for (int row = 0; row < 4; row++)
            {
                // Get non-zero tiles from previous grid in this row (left to right)
                var prevTiles = new List<(int col, int value)>();
                for (int col = 0; col < 4; col++)
                {
                    if (_previousGrid[row, col] != 0)
                    {
                        prevTiles.Add((col, _previousGrid[row, col]));
                    }
                }
                
                // Get non-zero tiles from new grid in this row (right to left, reversed)
                var newTilesInRow = new List<(int col, int value)>();
                for (int col = 3; col >= 0; col--)
                {
                    if (grid[row, col] != 0)
                    {
                        newTilesInRow.Add((col, grid[row, col]));
                    }
                }
                
                // Match tiles: last prev tile goes to last new position, etc.
                int prevIdx = prevTiles.Count - 1;
                int newIdx = newTilesInRow.Count - 1;
                
                while (prevIdx >= 0 && newIdx >= 0)
                {
                    var prevTile = prevTiles[prevIdx];
                    var newTile = newTilesInRow[newIdx];
                    
                    if (prevTile.value == newTile.value)
                    {
                        // Same tile, just moved
                        if (prevTile.col != newTile.col)
                        {
                            movements[(row, prevTile.col)] = (row, newTile.col);
                            usedFromPositions.Add((row, prevTile.col));
                        }
                        prevIdx--;
                        newIdx--;
                    }
                    else if (prevTile.value * 2 == newTile.value && prevIdx > 0 && 
                             prevTiles[prevIdx - 1].value == prevTile.value)
                    {
                        // Merged: two tiles merged into one
                        movements[(row, prevTile.col)] = (row, newTile.col);
                        movements[(row, prevTiles[prevIdx - 1].col)] = (row, newTile.col);
                        usedFromPositions.Add((row, prevTile.col));
                        usedFromPositions.Add((row, prevTiles[prevIdx - 1].col));
                        prevIdx -= 2;
                        newIdx--;
                    }
                    else
                    {
                        // New tile appeared
                        newTiles.Add((row, newTile.col));
                        newIdx--;
                    }
                }
                
                // Remaining new tiles are new
                while (newIdx >= 0)
                {
                    newTiles.Add((row, newTilesInRow[newIdx].col));
                    newIdx--;
                }
            }
        }

        private void TrackMovementsUp(int[,] grid, Dictionary<(int, int), (int, int)> movements, 
            List<(int, int)> newTiles, HashSet<(int, int)> usedFromPositions)
        {
            // For each column, track movements from bottom to top
            for (int col = 0; col < 4; col++)
            {
                // Get non-zero tiles from previous grid in this column (top to bottom)
                var prevTiles = new List<(int row, int value)>();
                for (int row = 0; row < 4; row++)
                {
                    if (_previousGrid[row, col] != 0)
                    {
                        prevTiles.Add((row, _previousGrid[row, col]));
                    }
                }
                
                // Get non-zero tiles from new grid in this column (top to bottom)
                var newTilesInCol = new List<(int row, int value)>();
                for (int row = 0; row < 4; row++)
                {
                    if (grid[row, col] != 0)
                    {
                        newTilesInCol.Add((row, grid[row, col]));
                    }
                }
                
                // Match tiles: first prev tile goes to first new position, etc.
                int prevIdx = 0;
                int newIdx = 0;
                
                while (prevIdx < prevTiles.Count && newIdx < newTilesInCol.Count)
                {
                    var prevTile = prevTiles[prevIdx];
                    var newTile = newTilesInCol[newIdx];
                    
                    if (prevTile.value == newTile.value)
                    {
                        // Same tile, just moved
                        if (prevTile.row != newTile.row)
                        {
                            movements[(prevTile.row, col)] = (newTile.row, col);
                            usedFromPositions.Add((prevTile.row, col));
                        }
                        prevIdx++;
                        newIdx++;
                    }
                    else if (prevTile.value * 2 == newTile.value && prevIdx + 1 < prevTiles.Count && 
                             prevTiles[prevIdx + 1].value == prevTile.value)
                    {
                        // Merged: two tiles merged into one
                        movements[(prevTile.row, col)] = (newTile.row, col);
                        movements[(prevTiles[prevIdx + 1].row, col)] = (newTile.row, col);
                        usedFromPositions.Add((prevTile.row, col));
                        usedFromPositions.Add((prevTiles[prevIdx + 1].row, col));
                        prevIdx += 2;
                        newIdx++;
                    }
                    else
                    {
                        // New tile appeared
                        newTiles.Add((newTile.row, col));
                        newIdx++;
                    }
                }
                
                // Remaining new tiles are new
                while (newIdx < newTilesInCol.Count)
                {
                    newTiles.Add((newTilesInCol[newIdx].row, col));
                    newIdx++;
                }
            }
        }

        private void TrackMovementsDown(int[,] grid, Dictionary<(int, int), (int, int)> movements, 
            List<(int, int)> newTiles, HashSet<(int, int)> usedFromPositions)
        {
            // For each column, track movements from top to bottom
            for (int col = 0; col < 4; col++)
            {
                // Get non-zero tiles from previous grid in this column (top to bottom)
                var prevTiles = new List<(int row, int value)>();
                for (int row = 0; row < 4; row++)
                {
                    if (_previousGrid[row, col] != 0)
                    {
                        prevTiles.Add((row, _previousGrid[row, col]));
                    }
                }
                
                // Get non-zero tiles from new grid in this column (bottom to top, reversed)
                var newTilesInCol = new List<(int row, int value)>();
                for (int row = 3; row >= 0; row--)
                {
                    if (grid[row, col] != 0)
                    {
                        newTilesInCol.Add((row, grid[row, col]));
                    }
                }
                
                // Match tiles: last prev tile goes to last new position, etc.
                int prevIdx = prevTiles.Count - 1;
                int newIdx = newTilesInCol.Count - 1;
                
                while (prevIdx >= 0 && newIdx >= 0)
                {
                    var prevTile = prevTiles[prevIdx];
                    var newTile = newTilesInCol[newIdx];
                    
                    if (prevTile.value == newTile.value)
                    {
                        // Same tile, just moved
                        if (prevTile.row != newTile.row)
                        {
                            movements[(prevTile.row, col)] = (newTile.row, col);
                            usedFromPositions.Add((prevTile.row, col));
                        }
                        prevIdx--;
                        newIdx--;
                    }
                    else if (prevTile.value * 2 == newTile.value && prevIdx > 0 && 
                             prevTiles[prevIdx - 1].value == prevTile.value)
                    {
                        // Merged: two tiles merged into one
                        movements[(prevTile.row, col)] = (newTile.row, col);
                        movements[(prevTiles[prevIdx - 1].row, col)] = (newTile.row, col);
                        usedFromPositions.Add((prevTile.row, col));
                        usedFromPositions.Add((prevTiles[prevIdx - 1].row, col));
                        prevIdx -= 2;
                        newIdx--;
                    }
                    else
                    {
                        // New tile appeared
                        newTiles.Add((newTile.row, col));
                        newIdx--;
                    }
                }
                
                // Remaining new tiles are new
                while (newIdx >= 0)
                {
                    newTiles.Add((newTilesInCol[newIdx].row, col));
                    newIdx--;
                }
            }
        }

        private void Game_ScoreChanged(object? sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void Game_GameOver(object? sender, EventArgs e)
        {
            GameOverOverlay.Visibility = Visibility.Visible;
            GameOverText.Text = "Game Over!";
            GameOverSubText.Text = "Press R to restart";
        }

        private void Game_GameWon(object? sender, EventArgs e)
        {
            if (!_game.IsGameOver)
            {
                GameOverOverlay.Visibility = Visibility.Visible;
                GameOverText.Text = "You Win!";
                GameOverSubText.Text = "Continue playing or press R to restart";
            }
        }

        private void NewGame()
        {
            _game.Reset();
            // Reset all transforms
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    var tile = _tiles[i, j];
                    var transformGroup = tile.RenderTransform as TransformGroup;
                    if (transformGroup != null)
                    {
                        var translateTransform = transformGroup.Children[0] as TranslateTransform;
                        var scaleTransform = transformGroup.Children[1] as ScaleTransform;
                        if (translateTransform != null)
                        {
                            translateTransform.X = 0;
                            translateTransform.Y = 0;
                        }
                        if (scaleTransform != null)
                        {
                            scaleTransform.ScaleX = 1;
                            scaleTransform.ScaleY = 1;
                        }
                    }
                }
            }
            UpdateDisplay();
            GameOverOverlay.Visibility = Visibility.Collapsed;
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            NewGame();
        }
    }
}

