using System.Collections.Generic;
using System.Text;
using ChessWPF.Models;

namespace ChessWPF.UI
{
    public static class PieceImages
    {
        // Unicode chess pieces — no external PNGs required; renders natively in WPF TextBlock
        public static string GetUnicode(Piece piece)
        {
            if (piece == null) return "";
            return (piece.Color, piece.Type) switch
            {
                (PieceColor.White, PieceType.King) => "♔",
                (PieceColor.White, PieceType.Queen) => "♕",
                (PieceColor.White, PieceType.Rook) => "♖",
                (PieceColor.White, PieceType.Bishop) => "♗",
                (PieceColor.White, PieceType.Knight) => "♘",
                (PieceColor.White, PieceType.Pawn) => "♙",
                (PieceColor.Black, PieceType.King) => "♚",
                (PieceColor.Black, PieceType.Queen) => "♛",
                (PieceColor.Black, PieceType.Rook) => "♜",
                (PieceColor.Black, PieceType.Bishop) => "♝",
                (PieceColor.Black, PieceType.Knight) => "♞",
                (PieceColor.Black, PieceType.Pawn) => "♟",
                _ => ""
            };
        }

        public static string GetRussianName(PieceType type) => type switch
        {
            PieceType.King => "Кор",
            PieceType.Queen => "Фер",
            PieceType.Rook => "Лад",
            PieceType.Bishop => "Сло",
            PieceType.Knight => "Кон",
            PieceType.Pawn => "Пеш",
            _ => "?"
        };

        public static string CapturedString(List<Piece> pieces)
        {
            var sb = new StringBuilder();
            foreach (var p in pieces)
                sb.Append(GetUnicode(p));
            return sb.ToString();
        }
    }
}
