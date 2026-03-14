namespace ChessWPF.Models
{
    public class Piece
    {
        public PieceType Type { get; set; }
        public PieceColor Color { get; set; }
        public bool HasMoved { get; set; }

        public Piece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
            HasMoved = false;
        }

        public Piece Clone()
        {
            return new Piece(Type, Color) { HasMoved = HasMoved };
        }

        public int GetValue()
        {
            return Type switch
            {
                PieceType.Pawn => 100,
                PieceType.Knight => 320,
                PieceType.Bishop => 330,
                PieceType.Rook => 500,
                PieceType.Queen => 900,
                PieceType.King => 20000,
                _ => 0
            };
        }
    }
}
