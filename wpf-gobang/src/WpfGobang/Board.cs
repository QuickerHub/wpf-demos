using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfGobang
{
    /// <summary>
    /// 五子棋棋盘类，管理棋盘状态和游戏逻辑
    /// 内部使用位棋盘（BitBoard）加速操作
    /// </summary>
    public class Board
    {
        public const int BOARD_SIZE = 15; // 15x15 棋盘
        private readonly Piece[,] _grid; // 保留用于兼容性
        private readonly BitBoard _bitBoard; // 位棋盘，用于加速操作

        public Board()
        {
            _grid = new Piece[BOARD_SIZE, BOARD_SIZE];
            _bitBoard = new BitBoard();
            Clear();
        }

        /// <summary>
        /// 从位棋盘创建（用于快速复制）
        /// </summary>
        private Board(BitBoard bitBoard)
        {
            _grid = new Piece[BOARD_SIZE, BOARD_SIZE];
            _bitBoard = bitBoard.Clone();
            // 同步到_grid（用于兼容）
            var grid = _bitBoard.ToGrid();
            Array.Copy(grid, _grid, BOARD_SIZE * BOARD_SIZE);
        }

        /// <summary>
        /// 清空棋盘
        /// </summary>
        public void Clear()
        {
            _bitBoard.Clear();
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    _grid[i, j] = Piece.None;
                }
            }
        }

        /// <summary>
        /// 获取指定位置的棋子（使用位棋盘加速）
        /// </summary>
        public Piece GetPiece(int row, int col)
        {
            if (IsValidPosition(row, col))
                return _bitBoard.GetPiece(row, col);
            return Piece.None;
        }

        /// <summary>
        /// 放置棋子（使用位棋盘加速）
        /// </summary>
        public bool PlacePiece(int row, int col, Piece piece)
        {
            if (!IsValidPosition(row, col))
                return false;

            if (_bitBoard.PlacePiece(row, col, piece))
            {
                _grid[row, col] = piece; // 同步到_grid
                return true;
            }
            return false;
        }

        /// <summary>
        /// 移除棋子（使用位棋盘加速）
        /// </summary>
        public void RemovePiece(int row, int col)
        {
            if (IsValidPosition(row, col))
            {
                _bitBoard.RemovePiece(row, col);
                _grid[row, col] = Piece.None; // 同步到_grid
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
        /// 检查是否有五子连珠（使用位棋盘加速）
        /// </summary>
        public Piece CheckWinner()
        {
            return _bitBoard.CheckWinner();
        }

        /// <summary>
        /// 检查棋盘是否已满（使用位棋盘加速）
        /// </summary>
        public bool IsFull()
        {
            return _bitBoard.IsFull();
        }

        /// <summary>
        /// 获取所有空位置（使用位棋盘加速）
        /// </summary>
        public List<(int row, int col)> GetEmptyPositions()
        {
            return _bitBoard.GetEmptyPositions();
        }

        /// <summary>
        /// 获取棋盘副本（用于AI计算）
        /// </summary>
        public Piece[,] GetGridCopy()
        {
            var copy = new Piece[BOARD_SIZE, BOARD_SIZE];
            Array.Copy(_grid, copy, BOARD_SIZE * BOARD_SIZE);
            return copy;
        }

        /// <summary>
        /// 从网格数组创建棋盘副本（更高效的复制方式）
        /// </summary>
        public void CopyFrom(Piece[,] grid)
        {
            if (grid == null || grid.GetLength(0) != BOARD_SIZE || grid.GetLength(1) != BOARD_SIZE)
                return;
            
            Array.Copy(grid, _grid, BOARD_SIZE * BOARD_SIZE);
            // 同步到位棋盘
            _bitBoard.Clear();
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    if (grid[i, j] != Piece.None)
                    {
                        _bitBoard.PlacePiece(i, j, grid[i, j]);
                    }
                }
            }
        }

        /// <summary>
        /// 快速复制棋盘（使用位棋盘加速）
        /// </summary>
        public Board Clone()
        {
            return new Board(_bitBoard);
        }

        /// <summary>
        /// 获取有棋子的位置周围的有效空位（使用位棋盘加速）
        /// </summary>
        public List<(int row, int col)> GetCandidatePositions()
        {
            return _bitBoard.GetCandidatePositions();
        }
    }
}

