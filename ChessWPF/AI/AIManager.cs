using System;
using System.Collections.Generic;
using ChessWPF.GameLogic;
using ChessWPF.Models;

namespace ChessWPF.AI
{
    public static class AIManager
    {
        // Piece-square tables for positional evaluation (White perspective, mirrored for Black)
        private static readonly int[,] PawnTable = {
            { 0,  0,  0,  0,  0,  0,  0,  0},
            {50, 50, 50, 50, 50, 50, 50, 50},
            {10, 10, 20, 30, 30, 20, 10, 10},
            { 5,  5, 10, 25, 25, 10,  5,  5},
            { 0,  0,  0, 20, 20,  0,  0,  0},
            { 5, -5,-10,  0,  0,-10, -5,  5},
            { 5, 10, 10,-20,-20, 10, 10,  5},
            { 0,  0,  0,  0,  0,  0,  0,  0}
        };

        private static readonly int[,] KnightTable = {
            {-50,-40,-30,-30,-30,-30,-40,-50},
            {-40,-20,  0,  0,  0,  0,-20,-40},
            {-30,  0, 10, 15, 15, 10,  0,-30},
            {-30,  5, 15, 20, 20, 15,  5,-30},
            {-30,  0, 15, 20, 20, 15,  0,-30},
            {-30,  5, 10, 15, 15, 10,  5,-30},
            {-40,-20,  0,  5,  5,  0,-20,-40},
            {-50,-40,-30,-30,-30,-30,-40,-50}
        };

        private static readonly int[,] BishopTable = {
            {-20,-10,-10,-10,-10,-10,-10,-20},
            {-10,  0,  0,  0,  0,  0,  0,-10},
            {-10,  0,  5, 10, 10,  5,  0,-10},
            {-10,  5,  5, 10, 10,  5,  5,-10},
            {-10,  0, 10, 10, 10, 10,  0,-10},
            {-10, 10, 10, 10, 10, 10, 10,-10},
            {-10,  5,  0,  0,  0,  0,  5,-10},
            {-20,-10,-10,-10,-10,-10,-10,-20}
        };

        private static readonly int[,] RookTable = {
            { 0,  0,  0,  0,  0,  0,  0,  0},
            { 5, 10, 10, 10, 10, 10, 10,  5},
            {-5,  0,  0,  0,  0,  0,  0, -5},
            {-5,  0,  0,  0,  0,  0,  0, -5},
            {-5,  0,  0,  0,  0,  0,  0, -5},
            {-5,  0,  0,  0,  0,  0,  0, -5},
            {-5,  0,  0,  0,  0,  0,  0, -5},
            { 0,  0,  0,  5,  5,  0,  0,  0}
        };

        private static readonly int[,] QueenTable = {
            {-20,-10,-10, -5, -5,-10,-10,-20},
            {-10,  0,  0,  0,  0,  0,  0,-10},
            {-10,  0,  5,  5,  5,  5,  0,-10},
            { -5,  0,  5,  5,  5,  5,  0, -5},
            {  0,  0,  5,  5,  5,  5,  0, -5},
            {-10,  5,  5,  5,  5,  5,  0,-10},
            {-10,  0,  5,  0,  0,  0,  0,-10},
            {-20,-10,-10, -5, -5,-10,-10,-20}
        };

        private static readonly int[,] KingMidTable = {
            {-30,-40,-40,-50,-50,-40,-40,-30},
            {-30,-40,-40,-50,-50,-40,-40,-30},
            {-30,-40,-40,-50,-50,-40,-40,-30},
            {-30,-40,-40,-50,-50,-40,-40,-30},
            {-20,-30,-30,-40,-40,-30,-30,-20},
            {-10,-20,-20,-20,-20,-20,-20,-10},
            { 20, 20,  0,  0,  0,  0, 20, 20},
            { 20, 30, 10,  0,  0, 10, 30, 20}
        };

        // ── public entry point ────────────────────────────────────────────
        public static Move GetBestMove(Board board, PieceColor color, int depth = 3)
        {
            var legalMoves = MoveValidator.GetAllLegalMoves(board, color);
            if (legalMoves.Count == 0) return null;

            Move bestMove = null;
            int bestScore = int.MinValue;
            bool maximizing = color == PieceColor.White;

            foreach (var move in legalMoves)
            {
                var copy = board.Clone();
                copy.ApplyMoveLowLevel(move);
                int score = Minimax(copy, depth - 1, int.MinValue, int.MaxValue, !maximizing, color);
                if (bestMove == null || score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
            return bestMove;
        }

        // ── minimax + alpha-beta ──────────────────────────────────────────
        private static int Minimax(Board board, int depth, int alpha, int beta,
                                    bool maximizing, PieceColor aiColor)
        {
            if (depth == 0) return Evaluate(board, aiColor);

            var side = maximizing ? aiColor : Opponent(aiColor);
            var moves = MoveValidator.GetAllLegalMoves(board, side);

            if (moves.Count == 0)
            {
                if (MoveValidator.IsInCheck(board, side))
                    return maximizing ? -100000 + depth : 100000 - depth; // checkmate
                return 0; // stalemate
            }

            if (maximizing)
            {
                int best = int.MinValue;
                foreach (var m in moves)
                {
                    var copy = board.Clone();
                    copy.ApplyMoveLowLevel(m);
                    int val = Minimax(copy, depth - 1, alpha, beta, false, aiColor);
                    best = Math.Max(best, val);
                    alpha = Math.Max(alpha, val);
                    if (beta <= alpha) break; // prune
                }
                return best;
            }
            else
            {
                int best = int.MaxValue;
                foreach (var m in moves)
                {
                    var copy = board.Clone();
                    copy.ApplyMoveLowLevel(m);
                    int val = Minimax(copy, depth - 1, alpha, beta, true, aiColor);
                    best = Math.Min(best, val);
                    beta = Math.Min(beta, val);
                    if (beta <= alpha) break;
                }
                return best;
            }
        }

        // ── evaluation ───────────────────────────────────────────────────
        private static int Evaluate(Board board, PieceColor aiColor)
        {
            int score = 0;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var piece = board.GetPiece(r, c);
                    if (piece == null) continue;

                    int materialValue = piece.GetValue();
                    int positional = GetPositional(piece, r, c);
                    int total = materialValue + positional;

                    if (piece.Color == aiColor) score += total;
                    else score -= total;
                }
            }
            return score;
        }

        private static int GetPositional(Piece piece, int row, int col)
        {
            // Mirror table for Black
            int pr = piece.Color == PieceColor.White ? row : 7 - row;

            return piece.Type switch
            {
                PieceType.Pawn => PawnTable[pr, col],
                PieceType.Knight => KnightTable[pr, col],
                PieceType.Bishop => BishopTable[pr, col],
                PieceType.Rook => RookTable[pr, col],
                PieceType.Queen => QueenTable[pr, col],
                PieceType.King => KingMidTable[pr, col],
                _ => 0
            };
        }

        private static PieceColor Opponent(PieceColor c)
            => c == PieceColor.White ? PieceColor.Black : PieceColor.White;
    }
}
