using System;
using System.Collections.Generic;
using System.Text;
using ChessWPF.Models;

namespace ChessWPF.GameLogic
{
    public class GameManager
    {
        public Board Board { get; private set; }
        public PieceColor CurrentTurn { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsCheck { get; private set; }
        public bool IsCheckmate { get; private set; }
        public bool IsStalemate { get; private set; }
        public Move LastMove { get; private set; }

        public List<Piece> CapturedByWhite { get; } = new();
        public List<Piece> CapturedByBlack { get; } = new();

        private readonly StringBuilder _historyBuilder = new();
        private int _moveNumber = 1;

        public string MoveHistoryText => _historyBuilder.ToString();

        public event Action<string> OnGameOver;
        public event Action OnCheck;

        public GameManager()
        {
            Board = new Board();
            Board.SetupStartPosition();
            CurrentTurn = PieceColor.White;
        }

        public List<Move> GetLegalMoves(int row, int col)
            => MoveValidator.GetLegalMoves(Board, row, col);

        public void ApplyMove(Move move)
        {
            if (IsGameOver) return;

            // Record capture
            var captured = Board.GetPiece(move.ToRow, move.ToCol);
            if (move.IsEnPassant)
            {
                int captRow = CurrentTurn == PieceColor.White ? move.ToRow + 1 : move.ToRow - 1;
                captured = Board.GetPiece(captRow, move.ToCol);
            }
            if (captured != null)
            {
                if (CurrentTurn == PieceColor.White) CapturedByWhite.Add(captured.Clone());
                else CapturedByBlack.Add(captured.Clone());
            }

            // Record history
            var piece = Board.GetPiece(move.FromRow, move.FromCol);
            if (CurrentTurn == PieceColor.White)
                _historyBuilder.Append($"{_moveNumber}. {move.ToAlgebraic()} ");
            else
            {
                _historyBuilder.AppendLine(move.ToAlgebraic());
                _moveNumber++;
            }

            // Apply
            Board.ApplyMoveLowLevel(move);
            LastMove = move;
            CurrentTurn = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;

            // Check game state
            IsCheck = MoveValidator.IsInCheck(Board, CurrentTurn);
            if (MoveValidator.IsCheckmate(Board, CurrentTurn))
            {
                IsGameOver = true;
                IsCheckmate = true;
                string winner = CurrentTurn == PieceColor.White ? "Чёрные" : "Белые";
                OnGameOver?.Invoke($"МАТ! Победа — {winner}");
            }
            else if (MoveValidator.IsStalemate(Board, CurrentTurn))
            {
                IsGameOver = true;
                IsStalemate = true;
                OnGameOver?.Invoke("ПАТ — Ничья");
            }
            else if (IsCheck)
            {
                OnCheck?.Invoke();
            }
        }
    }
}
