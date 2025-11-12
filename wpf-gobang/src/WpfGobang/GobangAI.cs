using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WpfGobang
{
    /// <summary>
    /// 五子棋AI算法类，使用Minimax算法和Alpha-Beta剪枝实现
    /// </summary>
    public class GobangAI
    {
        private const int MAX_DEPTH = 3; // 最大搜索深度（可以根据性能调整）
        private const int WIN_SCORE = 100000; // 获胜分数
        private const int LOSE_SCORE = -100000; // 失败分数
        private const int DRAW_SCORE = 0; // 平局分数

        /// <summary>
        /// 游戏难度枚举
        /// </summary>
        public enum Difficulty
        {
            Easy,    // 简单：较浅的搜索深度
            Medium,  // 中等：中等搜索深度
            Hard     // 困难：较深的搜索深度
        }

        private readonly Difficulty _difficulty;
        private readonly Random _random;

        public GobangAI(Difficulty difficulty = Difficulty.Medium)
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
        public (int row, int col) GetBestMove(Board board, Piece aiPiece, Piece playerPiece)
        {
            var candidates = board.GetCandidatePositions();
            
            if (candidates.Count == 0)
            {
                var emptyPositions = board.GetEmptyPositions();
                if (emptyPositions.Count == 0)
                    return (-1, -1);
                return emptyPositions[_random.Next(emptyPositions.Count)];
            }

            // 根据难度选择搜索深度
            int maxDepth = _difficulty switch
            {
                Difficulty.Easy => 2,
                Difficulty.Medium => 3,
                Difficulty.Hard => 4,
                _ => 3
            };

            int bestScore = int.MinValue;
            (int row, int col) bestMove = candidates[0];

            // 对候选位置按评估分数排序，优先搜索更有希望的位置
            var sortedCandidates = candidates.OrderByDescending(pos => 
                EvaluatePosition(board, pos.row, pos.col, aiPiece, playerPiece)).ToList();

            // 并行评估候选位置（优化：只在候选位置较多时并行）
            if (sortedCandidates.Count > 4)
            {
                var results = new (int row, int col, int score)[sortedCandidates.Count];
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                Parallel.For(0, sortedCandidates.Count, parallelOptions, i =>
                {
                    var pos = sortedCandidates[i];
                    // 为每个线程创建棋盘副本
                    var boardCopy = CreateBoardCopy(board);

                    // 尝试这个移动
                    boardCopy.PlacePiece(pos.row, pos.col, aiPiece);
                    
                    // 使用Alpha-Beta剪枝计算这个移动的分数
                    int score = AlphaBeta(boardCopy, 0, maxDepth, int.MinValue, int.MaxValue, false, aiPiece, playerPiece);
                    
                    results[i] = (pos.row, pos.col, score);
                });

                // 找到最佳移动
                foreach (var (row, col, score) in results)
                {
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = (row, col);
                    }
                }
            }
            else
            {
                // 候选位置较少时，使用串行计算（避免并行开销）
                foreach (var (row, col) in sortedCandidates)
                {
                    board.PlacePiece(row, col, aiPiece);
                    int score = AlphaBeta(board, 0, maxDepth, int.MinValue, int.MaxValue, false, aiPiece, playerPiece);
                    board.RemovePiece(row, col);
                    
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = (row, col);
                    }
                }
            }

            return bestMove;
        }

        /// <summary>
        /// Alpha-Beta剪枝算法实现
        /// </summary>
        private int AlphaBeta(Board board, int depth, int maxDepth, int alpha, int beta, 
            bool isMaximizing, Piece aiPiece, Piece playerPiece)
        {
            // 检查游戏是否结束
            var winner = board.CheckWinner();
            if (winner == aiPiece)
                return WIN_SCORE - depth; // 越早获胜分数越高
            if (winner == playerPiece)
                return LOSE_SCORE + depth; // 越晚失败分数越高
            if (board.IsFull())
                return DRAW_SCORE;

            // 达到最大深度，使用评估函数
            if (depth >= maxDepth)
                return EvaluateBoard(board, aiPiece, playerPiece);

            var candidates = board.GetCandidatePositions();
            if (candidates.Count == 0)
            {
                var emptyPositions = board.GetEmptyPositions();
                if (emptyPositions.Count == 0)
                    return DRAW_SCORE;
                candidates = emptyPositions;
            }

            if (isMaximizing) // AI的回合
            {
                int maxScore = int.MinValue;
                foreach (var (row, col) in candidates)
                {
                    board.PlacePiece(row, col, aiPiece);
                    int score = AlphaBeta(board, depth + 1, maxDepth, alpha, beta, false, aiPiece, playerPiece);
                    board.RemovePiece(row, col);
                    
                    maxScore = Math.Max(maxScore, score);
                    alpha = Math.Max(alpha, score);
                    
                    // Alpha-Beta剪枝
                    if (beta <= alpha)
                        break;
                }
                return maxScore;
            }
            else // 玩家的回合
            {
                int minScore = int.MaxValue;
                foreach (var (row, col) in candidates)
                {
                    board.PlacePiece(row, col, playerPiece);
                    int score = AlphaBeta(board, depth + 1, maxDepth, alpha, beta, true, aiPiece, playerPiece);
                    board.RemovePiece(row, col);
                    
                    minScore = Math.Min(minScore, score);
                    beta = Math.Min(beta, score);
                    
                    // Alpha-Beta剪枝
                    if (beta <= alpha)
                        break;
                }
                return minScore;
            }
        }

        /// <summary>
        /// 评估当前棋盘局面的分数
        /// </summary>
        private int EvaluateBoard(Board board, Piece aiPiece, Piece playerPiece)
        {
            // 使用并行计算评估棋盘，使用线程局部变量避免锁竞争
            int totalScore = 0;
            
            // 并行评估所有可能的五子连线
            Parallel.For(0, Board.BOARD_SIZE, () => 0, (i, loopState, localScore) =>
            {
                for (int j = 0; j < Board.BOARD_SIZE; j++)
                {
                    if (board.GetPiece(i, j) != Piece.None)
                    {
                        // 评估四个方向的连线
                        localScore += EvaluateDirection(board, i, j, 0, 1, aiPiece, playerPiece);   // 水平
                        localScore += EvaluateDirection(board, i, j, 1, 0, aiPiece, playerPiece);   // 垂直
                        localScore += EvaluateDirection(board, i, j, 1, 1, aiPiece, playerPiece);   // 主对角线
                        localScore += EvaluateDirection(board, i, j, 1, -1, aiPiece, playerPiece);  // 副对角线
                    }
                }
                return localScore;
            }, localScore => 
            {
                // 线程安全地累加分数（使用 Interlocked 比 lock 更高效）
                System.Threading.Interlocked.Add(ref totalScore, localScore);
            });
            
            return totalScore;
        }

        /// <summary>
        /// 评估指定方向的连线分数
        /// </summary>
        private int EvaluateDirection(Board board, int row, int col, int dr, int dc, 
            Piece aiPiece, Piece playerPiece)
        {
            Piece currentPiece = board.GetPiece(row, col);
            if (currentPiece == Piece.None)
                return 0;

            int count = 1; // 当前棋子
            bool blockedLeft = false, blockedRight = false;

            // 向前检查
            for (int i = 1; i < 5; i++)
            {
                int newRow = row + dr * i;
                int newCol = col + dc * i;
                if (!board.IsValidPosition(newRow, newCol))
                {
                    blockedRight = true;
                    break;
                }
                Piece piece = board.GetPiece(newRow, newCol);
                if (piece == currentPiece)
                    count++;
                else if (piece != Piece.None)
                {
                    blockedRight = true;
                    break;
                }
                else
                    break;
            }

            // 向后检查
            for (int i = 1; i < 5; i++)
            {
                int newRow = row - dr * i;
                int newCol = col - dc * i;
                if (!board.IsValidPosition(newRow, newCol))
                {
                    blockedLeft = true;
                    break;
                }
                Piece piece = board.GetPiece(newRow, newCol);
                if (piece == currentPiece)
                    count++;
                else if (piece != Piece.None)
                {
                    blockedLeft = true;
                    break;
                }
                else
                    break;
            }

            // 根据连子数和是否被阻挡计算分数
            int score = GetScoreByCount(count, blockedLeft, blockedRight);
            
            // 如果是对手的棋子，返回负分
            if (currentPiece == playerPiece)
                return -score;
            
            return score;
        }

        /// <summary>
        /// 根据连子数和阻挡情况计算分数
        /// </summary>
        private int GetScoreByCount(int count, bool blockedLeft, bool blockedRight)
        {
            if (count >= 5)
                return WIN_SCORE;

            int score = 0;
            bool blocked = blockedLeft || blockedRight;

            switch (count)
            {
                case 4:
                    if (!blocked)
                        score = 10000; // 活四：必胜
                    else if (!blockedLeft && !blockedRight)
                        score = 0; // 这种情况不应该发生
                    else
                        score = 1000; // 冲四：需要防守
                    break;
                case 3:
                    if (!blocked)
                        score = 1000; // 活三：很有威胁
                    else
                        score = 100; // 眠三：有一定威胁
                    break;
                case 2:
                    if (!blocked)
                        score = 100; // 活二：有潜力
                    else
                        score = 10; // 眠二：潜力较小
                    break;
                case 1:
                    score = 1; // 单子：价值很小
                    break;
            }

            return score;
        }

        /// <summary>
        /// 创建棋盘副本（使用位棋盘快速复制）
        /// </summary>
        private Board CreateBoardCopy(Board original)
        {
            return original.Clone();
        }

        /// <summary>
        /// 评估某个位置的潜在价值（用于排序候选位置）
        /// </summary>
        private int EvaluatePosition(Board board, int row, int col, Piece aiPiece, Piece playerPiece)
        {
            int score = 0;
            
            // 检查这个位置周围是否有己方或对方的棋子
            var directions = new int[][] 
            {
                new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 1, 1 }, new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                int dr = dir[0];
                int dc = dir[1];
                
                // 检查这个方向上的棋子分布
                int aiCount = 0;
                int playerCount = 0;
                
                for (int i = -4; i <= 4; i++)
                {
                    if (i == 0) continue; // 跳过当前位置
                    int newRow = row + dr * i;
                    int newCol = col + dc * i;
                    if (board.IsValidPosition(newRow, newCol))
                    {
                        Piece piece = board.GetPiece(newRow, newCol);
                        if (piece == aiPiece)
                            aiCount++;
                        else if (piece == playerPiece)
                            playerCount++;
                    }
                }
                
                // 如果这个位置能形成连线，给予更高分数
                if (aiCount > 0)
                    score += aiCount * 10;
                if (playerCount > 0)
                    score += playerCount * 5; // 防守也很重要
            }
            
            return score;
        }
    }
}

