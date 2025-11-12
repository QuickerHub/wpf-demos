using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfGobang
{
    /// <summary>
    /// 位棋盘实现，使用位运算加速棋盘操作
    /// 
    /// 三状态表示方法：
    /// 每个位置使用2个位（黑子位 + 白子位）表示3个状态：
    /// - 00 (两个位都是0) = None (空)
    /// - 01 (只有黑子位是1) = Black (黑子)
    /// - 10 (只有白子位是1) = White (白子)
    /// - 11 (两个位都是1) = 无效状态（不应该出现）
    /// 
    /// 15x15 棋盘 = 225 个位置
    /// 每个位置需要2位 = 450 位
    /// 使用两个独立的位棋盘，每个225位，需要4个ulong（每个64位）
    /// 总内存：8个ulong = 64字节（相比传统数组225字节，节省72%）
    /// </summary>
    public class BitBoard
    {
        public const int BOARD_SIZE = 15;
        private const int BITS_PER_ULONG = 64;
        private const int ULONGS_NEEDED = 4; // 225 bits / 64 = 4 ulongs

        // 使用两个独立的位棋盘表示3个状态
        // _blackBits: 黑子的位置（1表示黑子，0表示非黑子）
        // _whiteBits: 白子的位置（1表示白子，0表示非白子）
        // 组合起来：00=None, 01=Black, 10=White, 11=无效
        private readonly ulong[] _blackBits;  // 4个ulong，225位
        private readonly ulong[] _whiteBits;  // 4个ulong，225位

        // 跟踪最后落子的位置（用于优化CheckWinner，只检查最后落子周围）
        private int _lastMoveRow = -1;
        private int _lastMoveCol = -1;

        // 预计算的掩码和偏移量
        private static readonly int[] BitOffsets = new int[BOARD_SIZE * BOARD_SIZE];
        private static readonly int[] UlongIndices = new int[BOARD_SIZE * BOARD_SIZE];
        private static readonly ulong[] BitMasks = new ulong[BOARD_SIZE * BOARD_SIZE];

        static BitBoard()
        {
            // 预计算每个位置的位偏移和掩码
            for (int row = 0; row < BOARD_SIZE; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    int index = row * BOARD_SIZE + col;
                    UlongIndices[index] = index / BITS_PER_ULONG;
                    int bitOffset = index % BITS_PER_ULONG;
                    BitOffsets[index] = bitOffset;
                    BitMasks[index] = 1UL << bitOffset;
                }
            }
        }

        public BitBoard()
        {
            _blackBits = new ulong[ULONGS_NEEDED];
            _whiteBits = new ulong[ULONGS_NEEDED];
        }

        /// <summary>
        /// 复制构造函数
        /// </summary>
        public BitBoard(BitBoard other)
        {
            _blackBits = new ulong[ULONGS_NEEDED];
            _whiteBits = new ulong[ULONGS_NEEDED];
            Array.Copy(other._blackBits, _blackBits, ULONGS_NEEDED);
            Array.Copy(other._whiteBits, _whiteBits, ULONGS_NEEDED);
            _lastMoveRow = other._lastMoveRow;
            _lastMoveCol = other._lastMoveCol;
        }

        /// <summary>
        /// 清空棋盘
        /// </summary>
        public void Clear()
        {
            Array.Clear(_blackBits, 0, ULONGS_NEEDED);
            Array.Clear(_whiteBits, 0, ULONGS_NEEDED);
            _lastMoveRow = -1;
            _lastMoveCol = -1;
        }

        /// <summary>
        /// 获取指定位置的棋子
        /// 使用两个位棋盘组合判断3个状态：
        /// - 黑子位=1, 白子位=0 → Black
        /// - 黑子位=0, 白子位=1 → White
        /// - 黑子位=0, 白子位=0 → None
        /// </summary>
        public Piece GetPiece(int row, int col)
        {
            if (!IsValidPosition(row, col))
                return Piece.None;

            int index = row * BOARD_SIZE + col;
            int ulongIndex = UlongIndices[index];
            ulong mask = BitMasks[index];

            bool hasBlack = (_blackBits[ulongIndex] & mask) != 0;
            bool hasWhite = (_whiteBits[ulongIndex] & mask) != 0;

            // 理论上不应该同时为1，但如果出现，优先返回黑子
            if (hasBlack)
                return Piece.Black;
            if (hasWhite)
                return Piece.White;
            return Piece.None;
        }

        /// <summary>
        /// 放置棋子
        /// 确保两个位棋盘不会同时设置（避免无效状态11）
        /// </summary>
        public bool PlacePiece(int row, int col, Piece piece)
        {
            if (!IsValidPosition(row, col) || GetPiece(row, col) != Piece.None)
                return false;

            int index = row * BOARD_SIZE + col;
            int ulongIndex = UlongIndices[index];
            ulong mask = BitMasks[index];

            if (piece == Piece.Black)
            {
                // 设置黑子位，清除白子位（确保不会出现11状态）
                _blackBits[ulongIndex] |= mask;
                _whiteBits[ulongIndex] &= ~mask;
            }
            else if (piece == Piece.White)
            {
                // 设置白子位，清除黑子位（确保不会出现11状态）
                _whiteBits[ulongIndex] |= mask;
                _blackBits[ulongIndex] &= ~mask;
            }

            // 记录最后落子位置（用于优化CheckWinner）
            _lastMoveRow = row;
            _lastMoveCol = col;

            return true;
        }

        /// <summary>
        /// 移除棋子
        /// </summary>
        public void RemovePiece(int row, int col)
        {
            if (!IsValidPosition(row, col))
                return;

            int index = row * BOARD_SIZE + col;
            int ulongIndex = UlongIndices[index];
            ulong mask = BitMasks[index];

            _blackBits[ulongIndex] &= ~mask;
            _whiteBits[ulongIndex] &= ~mask;

            // 如果移除的是最后落子位置，清除记录
            if (_lastMoveRow == row && _lastMoveCol == col)
            {
                _lastMoveRow = -1;
                _lastMoveCol = -1;
            }
        }

        /// <summary>
        /// 检查位置是否有效
        /// </summary>
        public bool IsValidPosition(int row, int col)
        {
            return row >= 0 && row < BOARD_SIZE && col >= 0 && col < BOARD_SIZE;
        }

        /// <summary>
        /// 检查是否有五子连珠（使用位运算优化）
        /// 优化：只检查最后落子位置周围，而不是整个棋盘
        /// </summary>
        public Piece CheckWinner()
        {
            // 如果没有最后落子记录，使用全棋盘检查
            if (_lastMoveRow < 0 || _lastMoveCol < 0)
            {
                if (CheckFiveInARow(_blackBits))
                    return Piece.Black;
                if (CheckFiveInARow(_whiteBits))
                    return Piece.White;
                return Piece.None;
            }

            // 优化：只检查最后落子位置周围（最多检查4个方向）
            Piece lastPiece = GetPiece(_lastMoveRow, _lastMoveCol);
            if (lastPiece == Piece.None)
                return Piece.None;

            ulong[] bits = lastPiece == Piece.Black ? _blackBits : _whiteBits;

            // 只检查最后落子位置的四个方向
            if (CheckDirectionFast(bits, _lastMoveRow, _lastMoveCol, 0, 1))   // 水平
                return lastPiece;
            if (CheckDirectionFast(bits, _lastMoveRow, _lastMoveCol, 1, 0))   // 垂直
                return lastPiece;
            if (CheckDirectionFast(bits, _lastMoveRow, _lastMoveCol, 1, 1))   // 主对角线
                return lastPiece;
            if (CheckDirectionFast(bits, _lastMoveRow, _lastMoveCol, 1, -1))  // 副对角线
                return lastPiece;

            return Piece.None;
        }

        /// <summary>
        /// 使用位运算检查是否有五子连珠（优化版本：只检查最近落子的位置周围）
        /// </summary>
        private bool CheckFiveInARow(ulong[] bits)
        {
            // 优化：只检查有棋子的位置，而不是遍历所有位置
            // 使用位运算快速找到有棋子的位置
            for (int ulongIdx = 0; ulongIdx < ULONGS_NEEDED; ulongIdx++)
            {
                ulong currentBits = bits[ulongIdx];
                if (currentBits == 0)
                    continue; // 这个ulong中没有棋子，跳过

                // 找到这个ulong中所有有棋子的位置
                int startIndex = ulongIdx * BITS_PER_ULONG;
                int endIndex = Math.Min(startIndex + BITS_PER_ULONG, BOARD_SIZE * BOARD_SIZE);

                for (int bitPos = 0; bitPos < BITS_PER_ULONG && startIndex + bitPos < endIndex; bitPos++)
                {
                    if ((currentBits & (1UL << bitPos)) == 0)
                        continue;

                    int index = startIndex + bitPos;
                    int row = index / BOARD_SIZE;
                    int col = index % BOARD_SIZE;

                    // 检查四个方向（只检查一次，避免重复）
                    if (CheckDirectionFast(bits, row, col, 0, 1))   // 水平
                        return true;
                    if (CheckDirectionFast(bits, row, col, 1, 0))   // 垂直
                        return true;
                    if (CheckDirectionFast(bits, row, col, 1, 1))   // 主对角线
                        return true;
                    if (CheckDirectionFast(bits, row, col, 1, -1))  // 副对角线
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查指定方向是否有五子连珠（优化版本：减少重复检查）
        /// </summary>
        private bool CheckDirection(ulong[] bits, int startRow, int startCol, int dr, int dc)
        {
            return CheckDirectionFast(bits, startRow, startCol, dr, dc);
        }

        /// <summary>
        /// 快速检查指定方向是否有五子连珠（内联优化）
        /// </summary>
        private bool CheckDirectionFast(ulong[] bits, int startRow, int startCol, int dr, int dc)
        {
            int count = 1; // 当前棋子本身

            // 向前检查（最多4步）
            for (int i = 1; i < 5; i++)
            {
                int newRow = startRow + dr * i;
                int newCol = startCol + dc * i;
                if (newRow < 0 || newRow >= BOARD_SIZE || newCol < 0 || newCol >= BOARD_SIZE)
                    break;

                int index = newRow * BOARD_SIZE + newCol;
                int ulongIndex = UlongIndices[index];
                ulong mask = BitMasks[index];

                if ((bits[ulongIndex] & mask) != 0)
                    count++;
                else
                    break;
            }

            // 向后检查（最多4步）
            for (int i = 1; i < 5; i++)
            {
                int newRow = startRow - dr * i;
                int newCol = startCol - dc * i;
                if (newRow < 0 || newRow >= BOARD_SIZE || newCol < 0 || newCol >= BOARD_SIZE)
                    break;

                int index = newRow * BOARD_SIZE + newCol;
                int ulongIndex = UlongIndices[index];
                ulong mask = BitMasks[index];

                if ((bits[ulongIndex] & mask) != 0)
                    count++;
                else
                    break;
            }

            return count >= 5;
        }

        /// <summary>
        /// 检查棋盘是否已满（使用位运算优化）
        /// </summary>
        public bool IsFull()
        {
            // 快速检查：前3个ulong应该全部被占用
            ulong fullMask = ulong.MaxValue;
            if ((_blackBits[0] | _whiteBits[0]) != fullMask ||
                (_blackBits[1] | _whiteBits[1]) != fullMask ||
                (_blackBits[2] | _whiteBits[2]) != fullMask)
                return false;

            // 第4个ulong只需要前33位（225 - 3*64 = 33）
            ulong mask4 = (1UL << 33) - 1;
            return ((_blackBits[3] | _whiteBits[3]) & mask4) == mask4;
        }

        /// <summary>
        /// 获取所有空位置（使用位运算优化）
        /// </summary>
        public List<(int row, int col)> GetEmptyPositions()
        {
            var emptyPositions = new List<(int row, int col)>();
            
            for (int row = 0; row < BOARD_SIZE; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    int index = row * BOARD_SIZE + col;
                    int ulongIndex = UlongIndices[index];
                    ulong mask = BitMasks[index];

                    // 检查这个位置是否为空（黑子和白子都没有）
                    if ((_blackBits[ulongIndex] & mask) == 0 && 
                        (_whiteBits[ulongIndex] & mask) == 0)
                    {
                        emptyPositions.Add((row, col));
                    }
                }
            }

            return emptyPositions;
        }

        /// <summary>
        /// 获取有棋子的位置周围的有效空位（使用位运算优化，减少遍历）
        /// </summary>
        public List<(int row, int col)> GetCandidatePositions()
        {
            var candidates = new HashSet<(int row, int col)>();
            var directions = new int[][]
            {
                new int[] { -1, -1 }, new int[] { -1, 0 }, new int[] { -1, 1 },
                new int[] { 0, -1 },                        new int[] { 0, 1 },
                new int[] { 1, -1 },  new int[] { 1, 0 },  new int[] { 1, 1 }
            };

            // 优化：使用位运算快速找到有棋子的位置，而不是遍历所有位置
            // 合并黑子和白子的位棋盘，快速找到所有有棋子的位置
            for (int ulongIdx = 0; ulongIdx < ULONGS_NEEDED; ulongIdx++)
            {
                ulong occupiedBits = _blackBits[ulongIdx] | _whiteBits[ulongIdx];
                if (occupiedBits == 0)
                    continue; // 这个ulong中没有棋子，跳过

                int startIndex = ulongIdx * BITS_PER_ULONG;
                int endIndex = Math.Min(startIndex + BITS_PER_ULONG, BOARD_SIZE * BOARD_SIZE);

                // 找到这个ulong中所有有棋子的位置
                for (int bitPos = 0; bitPos < BITS_PER_ULONG && startIndex + bitPos < endIndex; bitPos++)
                {
                    if ((occupiedBits & (1UL << bitPos)) == 0)
                        continue;

                    int index = startIndex + bitPos;
                    int row = index / BOARD_SIZE;
                    int col = index % BOARD_SIZE;

                    // 检查周围8个方向
                    foreach (var dir in directions)
                    {
                        int newRow = row + dir[0];
                        int newCol = col + dir[1];
                        if (newRow >= 0 && newRow < BOARD_SIZE && newCol >= 0 && newCol < BOARD_SIZE)
                        {
                            int newIndex = newRow * BOARD_SIZE + newCol;
                            int newUlongIndex = UlongIndices[newIndex];
                            ulong newMask = BitMasks[newIndex];

                            // 使用位运算快速检查是否为空
                            if ((_blackBits[newUlongIndex] & newMask) == 0 &&
                                (_whiteBits[newUlongIndex] & newMask) == 0)
                            {
                                candidates.Add((newRow, newCol));
                            }
                        }
                    }
                }
            }

            // 如果棋盘为空，返回中心位置
            if (candidates.Count == 0 && !IsFull())
            {
                candidates.Add((BOARD_SIZE / 2, BOARD_SIZE / 2));
            }

            return candidates.ToList();
        }

        /// <summary>
        /// 快速复制位棋盘（只需要复制几个ulong数组）
        /// </summary>
        public BitBoard Clone()
        {
            return new BitBoard(this);
        }

        /// <summary>
        /// 从传统棋盘数组创建位棋盘（用于兼容）
        /// </summary>
        public static BitBoard FromGrid(Piece[,] grid)
        {
            var bitBoard = new BitBoard();
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    if (grid[i, j] != Piece.None)
                    {
                        bitBoard.PlacePiece(i, j, grid[i, j]);
                    }
                }
            }
            return bitBoard;
        }

        /// <summary>
        /// 转换为传统棋盘数组（用于兼容）
        /// </summary>
        public Piece[,] ToGrid()
        {
            var grid = new Piece[BOARD_SIZE, BOARD_SIZE];
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    grid[i, j] = GetPiece(i, j);
                }
            }
            return grid;
        }
    }
}

