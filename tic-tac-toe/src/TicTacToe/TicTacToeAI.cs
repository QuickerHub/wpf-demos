using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToe
{
    /// <summary>
    /// 智能井字棋AI算法类，使用Minimax算法实现最优策略
    /// </summary>
    public class TicTacToeAI
    {
        private const int MAX_DEPTH = 9; // 最大搜索深度
        private const int WIN_SCORE = 10; // 获胜分数
        private const int LOSE_SCORE = -10; // 失败分数
        private const int DRAW_SCORE = 0; // 平局分数

        /// <summary>
        /// 游戏难度枚举
        /// </summary>
        public enum Difficulty
        {
            Easy,    // 简单：随机选择
            Medium,  // 中等：部分使用Minimax
            Hard     // 困难：完全使用Minimax
        }

        private readonly Difficulty _difficulty;
        private readonly Random _random;

        public TicTacToeAI(Difficulty difficulty = Difficulty.Hard)
        {
            _difficulty = difficulty;
            _random = new Random();
        }

        /// <summary>
        /// 获取AI的最佳移动位置
        /// </summary>
        /// <param name="board">当前棋盘状态</param>
        /// <param name="aiPiece">AI的棋子类型</param>
        /// <param name="playerPiece">玩家的棋子类型</param>
        /// <returns>最佳移动位置 (row, col)</returns>
        public (int row, int col) GetBestMove(Piece[,] board, Piece aiPiece, Piece playerPiece)
        {
            var emptyPositions = GetEmptyPositions(board);
            
            if (!emptyPositions.Any())
                return (-1, -1); // 没有空位置

            // 根据难度选择策略
            switch (_difficulty)
            {
                case Difficulty.Easy:
                    return GetRandomMove(emptyPositions);
                case Difficulty.Medium:
                    return GetMediumMove(board, emptyPositions, aiPiece, playerPiece);
                case Difficulty.Hard:
                default:
                    return GetOptimalMove(board, emptyPositions, aiPiece, playerPiece);
            }
        }

        /// <summary>
        /// 简单难度：随机选择
        /// </summary>
        private (int row, int col) GetRandomMove(List<(int row, int col)> emptyPositions)
        {
            int index = _random.Next(emptyPositions.Count);
            return emptyPositions[index];
        }

        /// <summary>
        /// 中等难度：50%概率使用Minimax，50%随机
        /// </summary>
        private (int row, int col) GetMediumMove(Piece[,] board, List<(int row, int col)> emptyPositions, 
            Piece aiPiece, Piece playerPiece)
        {
            if (_random.NextDouble() < 0.5)
            {
                return GetOptimalMove(board, emptyPositions, aiPiece, playerPiece);
            }
            else
            {
                return GetRandomMove(emptyPositions);
            }
        }

        /// <summary>
        /// 困难难度：使用Minimax算法获取最优移动
        /// </summary>
        private (int row, int col) GetOptimalMove(Piece[,] board, List<(int row, int col)> emptyPositions, 
            Piece aiPiece, Piece playerPiece)
        {
            int bestScore = int.MinValue;
            (int row, int col) bestMove = emptyPositions[0];

            foreach (var (row, col) in emptyPositions)
            {
                // 尝试这个移动
                board[row, col] = aiPiece;
                
                // 计算这个移动的分数
                int score = Minimax(board, 0, false, aiPiece, playerPiece);
                
                // 撤销移动
                board[row, col] = Piece.None;
                
                // 更新最佳移动
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = (row, col);
                }
            }

            return bestMove;
        }

        /// <summary>
        /// Minimax算法实现
        /// </summary>
        /// <param name="board">当前棋盘状态</param>
        /// <param name="depth">当前深度</param>
        /// <param name="isMaximizing">是否为最大化玩家（AI）</param>
        /// <param name="aiPiece">AI的棋子类型</param>
        /// <param name="playerPiece">玩家的棋子类型</param>
        /// <returns>当前局面的评估分数</returns>
        private int Minimax(Piece[,] board, int depth, bool isMaximizing, Piece aiPiece, Piece playerPiece)
        {
            // 检查游戏是否结束
            var winner = CheckWinner(board);
            if (winner != Piece.None)
            {
                if (winner == aiPiece)
                    return WIN_SCORE - depth; // 越早获胜分数越高
                else
                    return LOSE_SCORE + depth; // 越晚失败分数越高
            }

            // 检查是否平局
            if (IsBoardFull(board))
                return DRAW_SCORE;

            // 限制搜索深度以提高性能
            if (depth >= MAX_DEPTH)
                return EvaluateBoard(board, aiPiece, playerPiece);

            if (isMaximizing)
            {
                int maxScore = int.MinValue;
                var emptyPositions = GetEmptyPositions(board);
                
                foreach (var (row, col) in emptyPositions)
                {
                    board[row, col] = aiPiece;
                    int score = Minimax(board, depth + 1, false, aiPiece, playerPiece);
                    board[row, col] = Piece.None;
                    maxScore = Math.Max(maxScore, score);
                }
                return maxScore;
            }
            else
            {
                int minScore = int.MaxValue;
                var emptyPositions = GetEmptyPositions(board);
                
                foreach (var (row, col) in emptyPositions)
                {
                    board[row, col] = playerPiece;
                    int score = Minimax(board, depth + 1, true, aiPiece, playerPiece);
                    board[row, col] = Piece.None;
                    minScore = Math.Min(minScore, score);
                }
                return minScore;
            }
        }

        /// <summary>
        /// 检查是否有获胜者
        /// </summary>
        private Piece CheckWinner(Piece[,] board)
        {
            // 检查行
            for (int i = 0; i < 3; i++)
            {
                if (board[i, 0] != Piece.None && board[i, 0] == board[i, 1] && board[i, 1] == board[i, 2])
                    return board[i, 0];
            }

            // 检查列
            for (int j = 0; j < 3; j++)
            {
                if (board[0, j] != Piece.None && board[0, j] == board[1, j] && board[1, j] == board[2, j])
                    return board[0, j];
            }

            // 检查对角线
            if (board[0, 0] != Piece.None && board[0, 0] == board[1, 1] && board[1, 1] == board[2, 2])
                return board[0, 0];

            if (board[0, 2] != Piece.None && board[0, 2] == board[1, 1] && board[1, 1] == board[2, 0])
                return board[0, 2];

            return Piece.None;
        }

        /// <summary>
        /// 检查棋盘是否已满
        /// </summary>
        private bool IsBoardFull(Piece[,] board)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j] == Piece.None)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 获取所有空位置
        /// </summary>
        private List<(int row, int col)> GetEmptyPositions(Piece[,] board)
        {
            var emptyPositions = new List<(int row, int col)>();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j] == Piece.None)
                        emptyPositions.Add((i, j));
                }
            }
            return emptyPositions;
        }

        /// <summary>
        /// 评估当前棋盘局面的分数（用于深度限制时的启发式评估）
        /// </summary>
        private int EvaluateBoard(Piece[,] board, Piece aiPiece, Piece playerPiece)
        {
            // 评估所有可能的连线
            int score = EvaluateLine(board, 0, 0, 0, 1, 0, 2, aiPiece, playerPiece); // 第一行
            score += EvaluateLine(board, 1, 0, 1, 1, 1, 2, aiPiece, playerPiece); // 第二行
            score += EvaluateLine(board, 2, 0, 2, 1, 2, 2, aiPiece, playerPiece); // 第三行
            score += EvaluateLine(board, 0, 0, 1, 0, 2, 0, aiPiece, playerPiece); // 第一列
            score += EvaluateLine(board, 0, 1, 1, 1, 2, 1, aiPiece, playerPiece); // 第二列
            score += EvaluateLine(board, 0, 2, 1, 2, 2, 2, aiPiece, playerPiece); // 第三列
            score += EvaluateLine(board, 0, 0, 1, 1, 2, 2, aiPiece, playerPiece); // 主对角线
            score += EvaluateLine(board, 0, 2, 1, 1, 2, 0, aiPiece, playerPiece); // 副对角线

            return score;
        }

        /// <summary>
        /// 评估一条线的分数
        /// </summary>
        private int EvaluateLine(Piece[,] board, int r1, int c1, int r2, int c2, int r3, int c3, 
            Piece aiPiece, Piece playerPiece)
        {
            int score = 0;
            Piece[] line = { board[r1, c1], board[r2, c2], board[r3, c3] };

            int aiCount = line.Count(p => p == aiPiece);
            int playerCount = line.Count(p => p == playerPiece);
            int emptyCount = line.Count(p => p == Piece.None);

            // 如果这条线已经被对手占据，返回负分
            if (playerCount > 0 && aiCount == 0)
                return -playerCount * 2;

            // 如果这条线已经被AI占据，返回正分
            if (aiCount > 0 && playerCount == 0)
                return aiCount * 2;

            // 如果这条线是空的，返回0
            if (emptyCount == 3)
                return 0;

            // 如果这条线有混合情况，返回0（这种情况不应该发生）
            return 0;
        }
    }
}
