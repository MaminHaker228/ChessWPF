using System.Collections.Generic;
using ChessWPF.Models;

namespace ChessWPF.GameLogic
{
    public static class MoveGenerator
    {
        public static List<Move> GetPseudoLegalMoves(Board board, int row, int col)
        {
            var moves = new List<Move>();
            var piece = board.GetPiece(row, col);
            if (piece == null) return moves;

            switch (piece.Type)
            {
                case PieceType.Pawn: AddPawnMoves(board, row, col, piece.Color, moves); break;
                case PieceType.Rook: AddRookMoves(board, row, col, piece.Color, moves); break;
                case PieceType.Knight: AddKnightMoves(board, row, col, piece.Color, moves); break;
                case PieceType.Bishop: AddBishopMoves(board, row, col, piece.Color, moves); break;
                case PieceType.Queen: AddQueenMoves(board, row, col, piece.Color, moves); break;
                case PieceType.King: AddKingMoves(board, row, col, piece.Color, moves); break;
            }
            return moves;
        }

        // ── Pawn ──────────────────────────────────────────────────────────
        private static void AddPawnMoves(Board board, int row, int col,
                                         PieceColor color, List<Move> moves)
        {
            int dir = color == PieceColor.White ? -1 : 1;
            int startRow = color == PieceColor.White ? 6 : 1;
            int promoRow = color == PieceColor.White ? 0 : 7;

            // forward one
            int nr = row + dir;
            if (Board.InBounds(nr, col) && board.GetPiece(nr, col) == null)
            {
                AddPawnMove(moves, row, col, nr, col, nr == promoRow);
                // forward two from start
                if (row == startRow && board.GetPiece(row + dir * 2, col) == null)
                    moves.Add(new Move(row, col, row + dir * 2, col));
            }

            // diagonal captures
            foreach (int dc in new[] { -1, 1 })
            {
                int nc = col + dc;
                if (!Board.InBounds(nr, nc)) continue;

                var target = board.GetPiece(nr, nc);
                if (target != null && target.Color != color)
                    AddPawnMove(moves, row, col, nr, nc, nr == promoRow);

                // en passant
                if (board.EnPassantRow == nr && board.EnPassantCol == nc)
                    moves.Add(new Move(row, col, nr, nc, isEnPassant: true));
            }
        }

        private static void AddPawnMove(List<Move> moves, int fr, int fc, int tr, int tc, bool promo)
        {
            if (promo)
            {
                foreach (var pt in new[] {PieceType.Queen,PieceType.Rook,
                                           PieceType.Bishop,PieceType.Knight})
                    moves.Add(new Move(fr, fc, tr, tc, isPromotion: true, promotionPiece: pt));
            }
            else
            {
                moves.Add(new Move(fr, fc, tr, tc));
            }
        }

        // ── Rook ──────────────────────────────────────────────────────────
        private static void AddRookMoves(Board board, int row, int col,
                                          PieceColor color, List<Move> moves)
        {
            int[][] dirs = { new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 } };
            AddSlidingMoves(board, row, col, color, dirs, moves);
        }

        // ── Bishop ────────────────────────────────────────────────────────
        private static void AddBishopMoves(Board board, int row, int col,
                                            PieceColor color, List<Move> moves)
        {
            int[][] dirs = { new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 } };
            AddSlidingMoves(board, row, col, color, dirs, moves);
        }

        // ── Queen ─────────────────────────────────────────────────────────
        private static void AddQueenMoves(Board board, int row, int col,
                                           PieceColor color, List<Move> moves)
        {
            AddRookMoves(board, row, col, color, moves);
            AddBishopMoves(board, row, col, color, moves);
        }

        // ── Knight ────────────────────────────────────────────────────────
        private static void AddKnightMoves(Board board, int row, int col,
                                            PieceColor color, List<Move> moves)
        {
            int[] dr = { -2, -2, -1, -1, 1, 1, 2, 2 };
            int[] dc = { -1, 1, -2, 2, -2, 2, -1, 1 };
            for (int i = 0; i < 8; i++)
            {
                int nr = row + dr[i], nc = col + dc[i];
                if (!Board.InBounds(nr, nc)) continue;
                var t = board.GetPiece(nr, nc);
                if (t == null || t.Color != color)
                    moves.Add(new Move(row, col, nr, nc));
            }
        }

        // ── King ──────────────────────────────────────────────────────────
        private static void AddKingMoves(Board board, int row, int col,
                                          PieceColor color, List<Move> moves)
        {
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int nr = row + dr, nc = col + dc;
                    if (!Board.InBounds(nr, nc)) continue;
                    var t = board.GetPiece(nr, nc);
                    if (t == null || t.Color != color)
                        moves.Add(new Move(row, col, nr, nc));
                }

            // Castling
            AddCastlingMoves(board, row, col, color, moves);
        }

        private static void AddCastlingMoves(Board board, int row, int col,
                                              PieceColor color, List<Move> moves)
        {
            var king = board.GetPiece(row, col);
            if (king == null || king.HasMoved) return;

            // King-side
            var rookKS = board.GetPiece(row, 7);
            if (rookKS != null && !rookKS.HasMoved &&
                board.GetPiece(row, 5) == null && board.GetPiece(row, 6) == null)
            {
                var m = new Move(row, col, row, 6, isCastling: true);
                m.CastlingRookFromCol = 7; m.CastlingRookToCol = 5;
                moves.Add(m);
            }

            // Queen-side
            var rookQS = board.GetPiece(row, 0);
            if (rookQS != null && !rookQS.HasMoved &&
                board.GetPiece(row, 1) == null &&
                board.GetPiece(row, 2) == null &&
                board.GetPiece(row, 3) == null)
            {
                var m = new Move(row, col, row, 2, isCastling: true);
                m.CastlingRookFromCol = 0; m.CastlingRookToCol = 3;
                moves.Add(m);
            }
        }

        // ── sliding helper ────────────────────────────────────────────────
        private static void AddSlidingMoves(Board board, int row, int col,
                                             PieceColor color, int[][] dirs, List<Move> moves)
        {
            foreach (var d in dirs)
            {
                int nr = row + d[0], nc = col + d[1];
                while (Board.InBounds(nr, nc))
                {
                    var t = board.GetPiece(nr, nc);
                    if (t == null)
                    {
                        moves.Add(new Move(row, col, nr, nc));
                    }
                    else
                    {
                        if (t.Color != color) moves.Add(new Move(row, col, nr, nc));
                        break;
                    }
                    nr += d[0]; nc += d[1];
                }
            }
        }

        // ── all pseudo-legal for color ─────────────────────────────────────
        public static List<Move> GetAllPseudoLegal(Board board, PieceColor color)
        {
            var all = new List<Move>();
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var p = board.GetPiece(r, c);
                    if (p != null && p.Color == color)
                        all.AddRange(GetPseudoLegalMoves(board, r, c));
                }
            return all;
        }
    }
}
