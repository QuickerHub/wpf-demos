using System;
using System.Collections.Generic;
using System.Linq;

namespace Wpf2048
{
    /// <summary>
    /// 2048 游戏逻辑类
    /// </summary>
    public class Game2048
    {
        private const int GRID_SIZE = 4;
        private readonly int[,] _grid;
        private readonly Random _random;

        public int[,] Grid => (int[,])_grid.Clone();
        public int Score { get; private set; }
        public int BestScore { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsWon { get; private set; }

        public event EventHandler? GameOver;
        public event EventHandler? GameWon;
        public event EventHandler? ScoreChanged;

        public Game2048()
        {
            _grid = new int[GRID_SIZE, GRID_SIZE];
            _random = new Random();
            BestScore = LoadBestScore();
            Reset();
        }

        /// <summary>
        /// 重置游戏
        /// </summary>
        public void Reset()
        {
            // Clear grid
            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    _grid[i, j] = 0;
                }
            }

            Score = 0;
            IsGameOver = false;
            IsWon = false;

            // Add two initial tiles
            AddRandomTile();
            AddRandomTile();

            OnScoreChanged();
        }

        /// <summary>
        /// 移动方向枚举
        /// </summary>
        public enum Direction
        {
            Up,
            Down,
            Left,
            Right
        }

        /// <summary>
        /// 执行移动
        /// </summary>
        public bool Move(Direction direction)
        {
            if (IsGameOver) return false;

            int[,] previousGrid = (int[,])_grid.Clone();
            bool moved = false;

            switch (direction)
            {
                case Direction.Left:
                    moved = MoveLeft();
                    break;
                case Direction.Right:
                    moved = MoveRight();
                    break;
                case Direction.Up:
                    moved = MoveUp();
                    break;
                case Direction.Down:
                    moved = MoveDown();
                    break;
            }

            if (moved)
            {
                AddRandomTile();
                OnScoreChanged();

                // Check win condition
                if (!IsWon && HasWon())
                {
                    IsWon = true;
                    OnGameWon();
                }

                // Check game over
                if (CheckGameOver())
                {
                    IsGameOver = true;
                    SaveBestScore();
                    OnGameOver();
                }
            }

            return moved;
        }

        /// <summary>
        /// 向左移动
        /// </summary>
        private bool MoveLeft()
        {
            bool moved = false;
            for (int i = 0; i < GRID_SIZE; i++)
            {
                int[] row = new int[GRID_SIZE];
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    row[j] = _grid[i, j];
                }

                int[] merged = MergeRow(row);
                moved = moved || !row.SequenceEqual(merged);

                for (int j = 0; j < GRID_SIZE; j++)
                {
                    _grid[i, j] = merged[j];
                }
            }
            return moved;
        }

        /// <summary>
        /// 向右移动
        /// </summary>
        private bool MoveRight()
        {
            bool moved = false;
            for (int i = 0; i < GRID_SIZE; i++)
            {
                int[] row = new int[GRID_SIZE];
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    row[j] = _grid[i, GRID_SIZE - 1 - j];
                }

                int[] merged = MergeRow(row);
                moved = moved || !row.SequenceEqual(merged);

                for (int j = 0; j < GRID_SIZE; j++)
                {
                    _grid[i, GRID_SIZE - 1 - j] = merged[j];
                }
            }
            return moved;
        }

        /// <summary>
        /// 向上移动
        /// </summary>
        private bool MoveUp()
        {
            bool moved = false;
            for (int j = 0; j < GRID_SIZE; j++)
            {
                int[] col = new int[GRID_SIZE];
                for (int i = 0; i < GRID_SIZE; i++)
                {
                    col[i] = _grid[i, j];
                }

                int[] merged = MergeRow(col);
                moved = moved || !col.SequenceEqual(merged);

                for (int i = 0; i < GRID_SIZE; i++)
                {
                    _grid[i, j] = merged[i];
                }
            }
            return moved;
        }

        /// <summary>
        /// 向下移动
        /// </summary>
        private bool MoveDown()
        {
            bool moved = false;
            for (int j = 0; j < GRID_SIZE; j++)
            {
                int[] col = new int[GRID_SIZE];
                for (int i = 0; i < GRID_SIZE; i++)
                {
                    col[i] = _grid[GRID_SIZE - 1 - i, j];
                }

                int[] merged = MergeRow(col);
                moved = moved || !col.SequenceEqual(merged);

                for (int i = 0; i < GRID_SIZE; i++)
                {
                    _grid[GRID_SIZE - 1 - i, j] = merged[i];
                }
            }
            return moved;
        }

        /// <summary>
        /// 合并一行（向左合并）
        /// </summary>
        private int[] MergeRow(int[] row)
        {
            // Remove zeros
            List<int> nonZeros = row.Where(x => x != 0).ToList();
            List<int> merged = new List<int>();

            for (int i = 0; i < nonZeros.Count; i++)
            {
                if (i < nonZeros.Count - 1 && nonZeros[i] == nonZeros[i + 1])
                {
                    // Merge
                    int mergedValue = nonZeros[i] * 2;
                    merged.Add(mergedValue);
                    Score += mergedValue;
                    if (Score > BestScore)
                    {
                        BestScore = Score;
                    }
                    i++; // Skip next element
                }
                else
                {
                    merged.Add(nonZeros[i]);
                }
            }

            // Pad with zeros
            while (merged.Count < GRID_SIZE)
            {
                merged.Add(0);
            }

            return merged.ToArray();
        }

        /// <summary>
        /// 添加随机方块
        /// </summary>
        private void AddRandomTile()
        {
            List<(int row, int col)> emptyCells = new List<(int, int)>();
            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    if (_grid[i, j] == 0)
                    {
                        emptyCells.Add((i, j));
                    }
                }
            }

            if (emptyCells.Count > 0)
            {
                var (row, col) = emptyCells[_random.Next(emptyCells.Count)];
                _grid[row, col] = _random.Next(10) < 9 ? 2 : 4; // 90% chance of 2, 10% chance of 4
            }
        }

        /// <summary>
        /// 检查是否获胜（达到2048）
        /// </summary>
        private bool HasWon()
        {
            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    if (_grid[i, j] == 2048)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 检查游戏是否结束
        /// </summary>
        private bool CheckGameOver()
        {
            // Check for empty cells
            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    if (_grid[i, j] == 0)
                    {
                        return false;
                    }
                }
            }

            // Check for possible merges
            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    int current = _grid[i, j];
                    // Check right
                    if (j < GRID_SIZE - 1 && _grid[i, j + 1] == current)
                        return false;
                    // Check down
                    if (i < GRID_SIZE - 1 && _grid[i + 1, j] == current)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 加载最高分
        /// </summary>
        private int LoadBestScore()
        {
            try
            {
                string scoreFile = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Wpf2048",
                    "bestscore.txt");
                if (System.IO.File.Exists(scoreFile))
                {
                    string content = System.IO.File.ReadAllText(scoreFile);
                    if (int.TryParse(content, out int score))
                    {
                        return score;
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            return 0;
        }

        /// <summary>
        /// 保存最高分
        /// </summary>
        private void SaveBestScore()
        {
            try
            {
                string appDataDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Wpf2048");
                if (!System.IO.Directory.Exists(appDataDir))
                {
                    System.IO.Directory.CreateDirectory(appDataDir);
                }
                string scoreFile = System.IO.Path.Combine(appDataDir, "bestscore.txt");
                System.IO.File.WriteAllText(scoreFile, BestScore.ToString());
            }
            catch
            {
                // Ignore errors
            }
        }

        protected virtual void OnGameOver()
        {
            GameOver?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnGameWon()
        {
            GameWon?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnScoreChanged()
        {
            ScoreChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

