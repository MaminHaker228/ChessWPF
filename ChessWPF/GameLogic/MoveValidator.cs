using System.Collections.Generic;
using ChessWPF.Models;

namespace ChessWPF.GameLogic
{
    public static class MoveValidator
    {
        // Returns true if the given color's king is in check on this board
        public static bool IsInCheck(Board board, PieceColor color)
        {
            var (kr, kc) = board.FindKing(color);
            if (kr == -1) return false;
            return IsSquareAttackedBy(board, kr, kc, Opponent(color));
        }

        public static bool IsSquareAttackedBy(Board board, int row, int col, PieceColor attacker)
        {
            var pseudo = MoveGenerator.GetAllPseudoLegal(board, attacker);
            foreach (var m in pseudo)
                if (m.ToRow == row && m.ToCol == col) return true;
            return false;
        }

        // Filters pseudo-legal moves to only legal ones (no self-check, castling through check)
        public static List<Move> GetLegalMoves(Board board, int row, int col)
        {
            var pseudo = MoveGenerator.GetPseudoLegalMoves(board, row, col);
            var legal = new List<Move>();
            var piece = board.GetPiece(row, col);
            if (piece == null) return legal;

            foreach (var move in pseudo)
            {
                // Castling: can't castle through or out of check
                if (move.IsCastling)
                {
                    if (IsInCheck(board, piece.Color)) continue;

                    // check squares king passes through
                    int direction = move.ToCol > move.FromCol ? 1 : -1;
                    int passCol = move.FromCol + direction;
                    bool throughCheck = false;
                    while (passCol != move.ToCol + direction)
                    {
                        var testBoard2 = board.Clone();
                        testBoard2.SetPiece(move.FromRow, passCol, testBoard2.GetPiece(move.FromRow, move.FromCol));
                        testBoard2.RemovePiece(move.FromRow, move.FromCol);
                        if (IsInCheck(testBoard2, piece.Color)) { throughCheck = true; break; }
                        passCol += direction;
                    }
                    if (throughCheck) continue;
                }

                var testBoard = board.Clone();
                testBoard.ApplyMoveLowLevel(move);
                if (!IsInCheck(testBoard, piece.Color))
                    legal.Add(move);
            }
            return legal;
        }

        public static List<Move> GetAllLegalMoves(Board board, PieceColor color)
        {
            var all = new List<Move>();
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var p = board.GetPiece(r, c);
                    if (p != null && p.Color == color)
                        all.AddRange(GetLegalMoves(board, r, c));
                }
            return all;
        }

        public static bool IsCheckmate(Board board, PieceColor color)
            => IsInCheck(board, color) && GetAllLegalMoves(board, color).Count == 0;

        public static bool IsStalemate(Board board, PieceColor color)
            => !IsInCheck(board, color) && GetAllLegalMoves(board, color).Count == 0;

        private static PieceColor Opponent(PieceColor c)
            => c == PieceColor.White ? PieceColor.Black : PieceColor.White;
    }
}
