namespace ChessWPF.Models
{
    public class Move
    {
        public int FromRow { get; }
        public int FromCol { get; }
        public int ToRow { get; }
        public int ToCol { get; }
        public bool IsPromotion { get; set; }
        public PieceType PromotionPiece { get; set; }
        public bool IsEnPassant { get; set; }
        public bool IsCastling { get; set; }
        public int CastlingRookFromCol { get; set; }
        public int CastlingRookToCol { get; set; }

        public Move(int fromRow, int fromCol, int toRow, int toCol,
                    bool isPromotion = false,
                    PieceType promotionPiece = PieceType.Queen,
                    bool isEnPassant = false,
                    bool isCastling = false)
        {
            FromRow = fromRow;
            FromCol = fromCol;
            ToRow = toRow;
            ToCol = toCol;
            IsPromotion = isPromotion;
            PromotionPiece = promotionPiece;
            IsEnPassant = isEnPassant;
            IsCastling = isCastling;
        }

        public string ToAlgebraic()
        {
            char fc = (char)('a' + FromCol);
            char tc = (char)('a' + ToCol);
            int fr = 8 - FromRow;
            int tr = 8 - ToRow;
            string s = $"{fc}{fr}{tc}{tr}";
            if (IsPromotion) s += PromotionPiece switch
            {
                PieceType.Queen => "q",
                PieceType.Rook => "r",
                PieceType.Bishop => "b",
                PieceType.Knight => "n",
                _ => "q"
            };
            return s;
        }
    }
}
