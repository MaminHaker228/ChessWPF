using System;
using System.Collections.Generic;
using System.Linq;
using ChessWPF.GameLogic;
using ChessWPF.Models;

namespace ChessWPF.AI
{
    public static class AIManager
    {
        // ── настройки ─────────────────────────────────────────────────────
        private const int MAX_DEPTH = 5;   // глубина поиска
        private const int CHECKMATE_VAL = 100000;
        private const int INFINITY = 999999;

        // ── таблицы позиционной оценки (белые, зеркалятся для чёрных) ────

        private static readonly int[] PawnTable = {
             0,  0,  0,  0,  0,  0,  0,  0,
            98,134, 61, 95, 68,126, 34,-11,
            -6,  7, 26, 31, 65, 56, 25,-20,
           -14, 13,  6, 21, 23, 12, 17,-23,
           -27, -2, -5, 12, 17,  6, 10,-25,
           -26, -4, -4,-10,  3,  3, 33,-12,
           -35, -1,-20,-23,-15, 24, 38,-22,
             0,  0,  0,  0,  0,  0,  0,  0
        };

        private static readonly int[] KnightTable = {
           -167,-89,-34,-49, 61,-97,-15,-107,
            -73,-41, 72, 36, 23, 62,  7, -17,
            -47, 60, 37, 65, 84,129, 73,  44,
             -9, 17, 19, 53, 37, 69, 18,  22,
            -13,  4, 16, 13, 28, 19, 21,  -8,
            -23, -9, 12, 10, 19, 17, 25, -16,
            -29,-53,-12, -3, -1, 18,-14, -19,
           -105,-21,-58,-33,-17,-28,-19, -23
        };

        private static readonly int[] BishopTable = {
            -29,  4,-82,-37,-25,-42,  7, -8,
            -26, 16,-18,-13, 30, 59, 18,-47,
            -16, 37, 43, 40, 35, 50, 37, -2,
             -4,  5, 19, 50, 37, 37,  7, -2,
             -6, 13, 13, 26, 34, 12, 10,  4,
              0, 15, 15, 15, 14, 27, 18, 10,
              4, 15, 16,  0,  7, 21, 33,  1,
            -33, -3,-14,-21,-13,-12,-39,-21
        };

        private static readonly int[] RookTable = {
             32, 42, 32, 51, 63,  9, 31, 43,
             27, 32, 58, 62, 80, 67, 26, 44,
             -5, 19, 26, 36, 17, 45, 61, 16,
            -24,-11,  7, 26, 24, 35, -8,-20,
            -36,-26,-12, -1,  9, -7,  6,-23,
            -45,-25,-16,-17,  3,  0, -5,-33,
            -44,-16,-20, -9, -1, 11, -6,-71,
            -19,-13,  1, 17, 16,  7,-37,-26
        };

        private static readonly int[] QueenTable = {
            -28,  0, 29, 12, 59, 44, 43, 45,
            -24,-39, -5,  1,-16, 57, 28, 54,
            -13,-17,  7,  8, 29, 56, 47, 57,
            -27,-27,-16,-16, -1, 17, -2,  1,
             -9,-26, -9,-10, -2, -4,  3, -3,
            -14,  2,-11, -2, -5,  2, 14,  5,
            -35, -8, 11,  2,  8, 15, -3,  1,
             -1,-18, -9, 10,-15,-25,-31,-50
        };

        private static readonly int[] KingMiddleTable = {
            -65, 23, 16,-15,-56,-34,  2, 13,
             29, -1,-20, -7, -8, -4,-38,-29,
             -9, 24,  2,-16,-20,  6, 22,-22,
            -17,-20,-12,-27,-30,-25,-14,-36,
            -49, -1,-27,-39,-46,-44,-33,-51,
            -14,-14,-22,-46,-44,-30,-15,-27,
              1,  7, -8,-64,-43,-16,  9,  8,
            -15, 36, 12,-54,  8,-28, 24, 14
        };

        private static readonly int[] KingEndTable = {
            -74,-35,-18,-18,-11, 15,  4,-17,
            -12, 17, 14, 17, 17, 38, 23, 11,
             10, 17, 23, 15, 20, 45, 44, 13,
             -8, 22, 24, 27, 26, 33, 26,  3,
            -18, -4, 21, 24, 27, 23,  9,-11,
            -19, -3, 11, 21, 23, 16,  7, -9,
            -27,-11,  4, 13, 14,  4,-5,-17,
            -53,-34,-21,-11,-28,-14,-24,-43
        };

        // ── Transposition Table ───────────────────────────────────────────
        private static readonly Dictionary<ulong, TTEntry> _tt = new(1 << 20);

        private struct TTEntry
        {
            public int Score;
            public int Depth;
            public TTFlag Flag;
            public Move BestMove;
        }

        private enum TTFlag { Exact, Alpha, Beta }

        // ── Killer moves ──────────────────────────────────────────────────
        private static readonly Move[,] _killers = new Move[2, 64];

        // ── History heuristic ─────────────────────────────────────────────
        private static readonly int[,,,] _history = new int[2, 64, 8, 8];

        // ── публичный вход ────────────────────────────────────────────────
        public static Move GetBestMove(Board board, PieceColor color, int depth = MAX_DEPTH)
        {
            // Очищаем killer moves и history для новой позиции
            Array.Clear(_killers, 0, _killers.Length);
            Array.Clear(_history, 0, _history.Length);

            Move bestMove = null;
            int bestScore = -INFINITY;

            // Iterative deepening — ищем сначала глубиной 1, потом 2, ... до MAX_DEPTH
            for (int d = 1; d <= depth; d++)
            {
                var (score, move) = SearchRoot(board, color, d);
                if (move != null)
                {
                    bestMove = move;
                    bestScore = score;
                }
                // Если нашли мат — нет смысла искать глубже
                if (Math.Abs(bestScore) > CHECKMATE_VAL - 100) break;
            }

            return bestMove;
        }

        // ── корневой поиск ────────────────────────────────────────────────
        private static (int score, Move move) SearchRoot(Board board, PieceColor color, int depth)
        {
            var moves = GetOrderedMoves(board, color, 0);
            if (moves.Count == 0) return (0, null);

            Move bestMove = moves[0];
            int bestScore = -INFINITY;

            foreach (var move in moves)
            {
                var copy = board.Clone();
                copy.ApplyMoveLowLevel(move);

                int score = -NegaMax(copy, depth - 1, -INFINITY, INFINITY,
                                     Opponent(color), 1);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
            return (bestScore, bestMove);
        }

        // ── NegaMax + Alpha-Beta + TT + Killers + History ─────────────────
        private static int NegaMax(Board board, int depth, int alpha, int beta,
                                   PieceColor color, int ply)
        {
            int alphaOrig = alpha;

            // Transposition Table lookup
            ulong hash = Zobrist.Hash(board, color);
            if (_tt.TryGetValue(hash, out var entry) && entry.Depth >= depth)
            {
                switch (entry.Flag)
                {
                    case TTFlag.Exact: return entry.Score;
                    case TTFlag.Alpha: alpha = Math.Max(alpha, entry.Score); break;
                    case TTFlag.Beta: beta = Math.Min(beta, entry.Score); break;
                }
                if (alpha >= beta) return entry.Score;
            }

            // Quiescence search на глубине 0
            if (depth <= 0)
                return Quiescence(board, alpha, beta, color, ply);

            var moves = GetOrderedMoves(board, color, ply);

            // Нет ходов — мат или пат
            if (moves.Count == 0)
            {
                if (MoveValidator.IsInCheck(board, color))
                    return -(CHECKMATE_VAL - ply); // мат — чем быстрее, тем лучше
                return 0; // пат
            }

            // Null move pruning (ускорение: пропускаем ход)
            if (depth >= 3 && !MoveValidator.IsInCheck(board, color))
            {
                var nullBoard = board.Clone();
                nullBoard.EnPassantRow = nullBoard.EnPassantCol = -1;
                int nullScore = -NegaMax(nullBoard, depth - 3, -beta, -beta + 1,
                                         Opponent(color), ply + 1);
                if (nullScore >= beta) return beta;
            }

            Move localBest = null;
            int best = -INFINITY;

            for (int i = 0; i < moves.Count; i++)
            {
                var move = moves[i];
                var copy = board.Clone();
                copy.ApplyMoveLowLevel(move);

                // Late Move Reduction — поздние ходы ищем менее глубоко
                int reduction = 0;
                if (i >= 4 && depth >= 3 && !move.IsPromotion &&
                    board.GetPiece(move.ToRow, move.ToCol) == null)
                    reduction = 1;

                int score = -NegaMax(copy, depth - 1 - reduction, -beta, -alpha,
                                     Opponent(color), ply + 1);

                // Re-search если LMR улучшил alpha
                if (reduction > 0 && score > alpha)
                    score = -NegaMax(copy, depth - 1, -beta, -alpha,
                                     Opponent(color), ply + 1);

                if (score > best)
                {
                    best = score;
                    localBest = move;
                }

                alpha = Math.Max(alpha, score);

                if (alpha >= beta)
                {
                    // Killer move — запоминаем тихий ход вызвавший отсечку
                    if (board.GetPiece(move.ToRow, move.ToCol) == null)
                    {
                        _killers[1, ply] = _killers[0, ply];
                        _killers[0, ply] = move;
                        int ci = color == PieceColor.White ? 0 : 1;
                        _history[ci, ply, move.ToRow, move.ToCol] += depth * depth;
                    }
                    break; // beta cutoff
                }
            }

            // Сохранить в TT
            var ttEntry = new TTEntry
            {
                Score = best,
                Depth = depth,
                BestMove = localBest,
                Flag = best <= alphaOrig ? TTFlag.Beta
                         : best >= beta ? TTFlag.Alpha
                                             : TTFlag.Exact
            };
            _tt[hash] = ttEntry;

            return best;
        }

        // ── Quiescence Search — оцениваем только взятия ───────────────────
        private static int Quiescence(Board board, int alpha, int beta,
                                       PieceColor color, int ply)
        {
            int standPat = EvaluateFromPerspective(board, color);
            if (standPat >= beta) return beta;
            if (standPat > alpha) alpha = standPat;

            // Только взятия
            var captures = GetOrderedMoves(board, color, ply)
                           .Where(m => board.GetPiece(m.ToRow, m.ToCol) != null
                                    || m.IsEnPassant)
                           .ToList();

            foreach (var move in captures)
            {
                var copy = board.Clone();
                copy.ApplyMoveLowLevel(move);
                int score = -Quiescence(copy, -beta, -alpha, Opponent(color), ply + 1);

                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }
            return alpha;
        }

        // ── упорядочивание ходов (MVV-LVA + Killers + History) ────────────
        private static List<Move> GetOrderedMoves(Board board, PieceColor color, int ply)
        {
            var legal = MoveValidator.GetAllLegalMoves(board, color);
            if (legal.Count == 0) return legal;

            // Оценка каждого хода для сортировки
            var scored = legal.Select(m => (move: m, score: ScoreMove(board, m, color, ply)))
                              .OrderByDescending(x => x.score)
                              .Select(x => x.move)
                              .ToList();
            return scored;
        }

        private static int ScoreMove(Board board, Move move, PieceColor color, int ply)
        {
            int score = 0;

            // 1. Взятие — MVV-LVA (Most Valuable Victim - Least Valuable Attacker)
            var victim = board.GetPiece(move.ToRow, move.ToCol);
            var attacker = board.GetPiece(move.FromRow, move.FromCol);
            if (victim != null)
            {
                score += 10 * victim.GetValue() - attacker.GetValue() + 100000;
            }

            // 2. Превращение пешки
            if (move.IsPromotion)
                score += move.PromotionPiece == PieceType.Queen ? 90000 : 50000;

            // 3. Killer moves
            if (_killers[0, ply] != null &&
                _killers[0, ply].FromRow == move.FromRow &&
                _killers[0, ply].FromCol == move.FromCol &&
                _killers[0, ply].ToRow == move.ToRow &&
                _killers[0, ply].ToCol == move.ToCol)
                score += 9000;

            if (_killers[1, ply] != null &&
                _killers[1, ply].FromRow == move.FromRow &&
                _killers[1, ply].FromCol == move.FromCol &&
                _killers[1, ply].ToRow == move.ToRow &&
                _killers[1, ply].ToCol == move.ToCol)
                score += 8000;

            // 4. History heuristic
            int ci = color == PieceColor.White ? 0 : 1;
            score += _history[ci, Math.Min(ply, 63), move.ToRow, move.ToCol];

            return score;
        }

        // ── оценка позиции ────────────────────────────────────────────────
        public static int EvaluateFromPerspective(Board board, PieceColor color)
        {
            int score = Evaluate(board);
            return color == PieceColor.White ? score : -score;
        }

        public static int Evaluate(Board board)
        {
            int whiteScore = 0, blackScore = 0;
            int whiteMaterial = 0, blackMaterial = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var piece = board.GetPiece(r, c);
                    if (piece == null) continue;

                    int mat = piece.GetValue();
                    int sq = r * 8 + c;

                    if (piece.Color == PieceColor.White)
                    {
                        whiteMaterial += mat;
                        whiteScore += mat + GetPST(piece, sq, true);
                    }
                    else
                    {
                        blackMaterial += mat;
                        blackScore += mat + GetPST(piece, sq, false);
                    }
                }
            }

            // Фаза игры (эндшпиль если мало материала)
            bool endgame = whiteMaterial + blackMaterial < 3000;

            // Бонус за мобильность (количество ходов)
            int whiteMobility = MoveGenerator.GetAllPseudoLegal(board, PieceColor.White).Count;
            int blackMobility = MoveGenerator.GetAllPseudoLegal(board, PieceColor.Black).Count;
            whiteScore += whiteMobility * 2;
            blackScore += blackMobility * 2;

            // Штраф за сдвоенные пешки
            whiteScore -= DoubledPawnPenalty(board, PieceColor.White);
            blackScore -= DoubledPawnPenalty(board, PieceColor.Black);

            // Бонус за пешечный центр
            whiteScore += PawnCenterBonus(board, PieceColor.White);
            blackScore += PawnCenterBonus(board, PieceColor.Black);

            // Безопасность короля
            if (!endgame)
            {
                whiteScore -= KingSafety(board, PieceColor.White);
                blackScore -= KingSafety(board, PieceColor.Black);
            }

            return whiteScore - blackScore;
        }

        private static int GetPST(Piece piece, int sq, bool white)
        {
            int idx = white ? sq : 63 - sq; // зеркало для чёрных
            return piece.Type switch
            {
                PieceType.Pawn => PawnTable[idx],
                PieceType.Knight => KnightTable[idx],
                PieceType.Bishop => BishopTable[idx],
                PieceType.Rook => RookTable[idx],
                PieceType.Queen => QueenTable[idx],
                PieceType.King => KingMiddleTable[idx],
                _ => 0
            };
        }

        // ── штраф за сдвоенные пешки ──────────────────────────────────────
        private static int DoubledPawnPenalty(Board board, PieceColor color)
        {
            int penalty = 0;
            for (int c = 0; c < 8; c++)
            {
                int count = 0;
                for (int r = 0; r < 8; r++)
                {
                    var p = board.GetPiece(r, c);
                    if (p != null && p.Type == PieceType.Pawn && p.Color == color)
                        count++;
                }
                if (count > 1) penalty += (count - 1) * 20;
            }
            return penalty;
        }

        // ── бонус за пешки в центре ───────────────────────────────────────
        private static int PawnCenterBonus(Board board, PieceColor color)
        {
            int bonus = 0;
            int[] centerRows = color == PieceColor.White ? new[] { 3, 4 } : new[] { 3, 4 };
            int[] centerCols = { 3, 4 };
            foreach (int r in centerRows)
                foreach (int c in centerCols)
                {
                    var p = board.GetPiece(r, c);
                    if (p != null && p.Type == PieceType.Pawn && p.Color == color)
                        bonus += 30;
                }
            return bonus;
        }

        // ── безопасность короля ───────────────────────────────────────────
        private static int KingSafety(Board board, PieceColor color)
        {
            var (kr, kc) = board.FindKing(color);
            if (kr < 0) return 0;

            int danger = 0;
            // Считаем атаки противника вокруг короля
            var opponent = Opponent(color);
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    int nr = kr + dr, nc = kc + dc;
                    if (!Board.InBounds(nr, nc)) continue;
                    if (MoveValidator.IsSquareAttackedBy(board, nr, nc, opponent))
                        danger += 10;
                }
            return danger;
        }

        private static PieceColor Opponent(PieceColor c)
            => c == PieceColor.White ? PieceColor.Black : PieceColor.White;
    }

    // ── Zobrist Hashing для Transposition Table ───────────────────────────
    public static class Zobrist
    {
        private static readonly ulong[,,] _table = new ulong[64, 12, 2];
        private static readonly ulong _blackToMove;
        private static readonly Random _rng = new(42);

        static Zobrist()
        {
            for (int sq = 0; sq < 64; sq++)
                for (int pt = 0; pt < 12; pt++)
                    for (int c = 0; c < 2; c++)
                        _table[sq, pt, c] = NextRand();
            _blackToMove = NextRand();
        }

        private static ulong NextRand()
        {
            var buf = new byte[8];
            _rng.NextBytes(buf);
            return BitConverter.ToUInt64(buf, 0);
        }

        public static ulong Hash(Board board, PieceColor sideToMove)
        {
            ulong h = 0;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var p = board.GetPiece(r, c);
                    if (p == null) continue;
                    int sq = r * 8 + c;
                    int pt = (int)p.Type;
                    int co = p.Color == PieceColor.White ? 0 : 1;
                    h ^= _table[sq, pt, co];
                }
            if (sideToMove == PieceColor.Black) h ^= _blackToMove;
            return h;
        }
    }
}
