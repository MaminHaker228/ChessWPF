using System;

namespace ChessWPF.Models
{
    public class Board
    {
        private readonly Piece[,] _squares = new Piece[8, 8];

        // En passant target square (row, col) or (-1,-1)
        public int EnPassantRow { get; set; } = -1;
        public int EnPassantCol { get; set; } = -1;

        public Board() { }

        // ?? access ????????????????????????????????????????????????????????
        public Piece GetPiece(int row, int col) => _squares[row, col];
        public void SetPiece(int row, int col, Piece p) => _squares[row, col] = p;
        public void RemovePiece(int row, int col) => _squares[row, col] = null;

        // ?? init standard position ?????????????????????????????????????????
        public void SetupStartPosition()
        {
            // Clear
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    _squares[r, c] = null;

            // Black pieces (row 0 = rank 8)
            SetPiece(0, 0, new Piece(PieceType.Rook, PieceColor.Black));
            SetPiece(0, 1, new Piece(PieceType.Knight, PieceColor.Black));
            SetPiece(0, 2, new Piece(PieceType.Bishop, PieceColor.Black));
            SetPiece(0, 3, new Piece(PieceType.Queen, PieceColor.Black));
            SetPiece(0, 4, new Piece(PieceType.King, PieceColor.Black));
            SetPiece(0, 5, new Piece(PieceType.Bishop, PieceColor.Black));
            SetPiece(0, 6, new Piece(PieceType.Knight, PieceColor.Black));
            SetPiece(0, 7, new Piece(PieceType.Rook, PieceColor.Black));
            for (int c = 0; c < 8; c++)
                SetPiece(1, c, new Piece(PieceType.Pawn, PieceColor.Black));

            // White pieces (row 7 = rank 1)
            SetPiece(7, 0, new Piece(PieceType.Rook, PieceColor.White));
            SetPiece(7, 1, new Piece(PieceType.Knight, PieceColor.White));
            SetPiece(7, 2, new Piece(PieceType.Bishop, PieceColor.White));
            SetPiece(7, 3, new Piece(PieceType.Queen, PieceColor.White));
            SetPiece(7, 4, new Piece(PieceType.King, PieceColor.White));
            SetPiece(7, 5, new Piece(PieceType.Bishop, PieceColor.White));
            SetPiece(7, 6, new Piece(PieceType.Knight, PieceColor.White));
            SetPiece(7, 7, new Piece(PieceType.Rook, PieceColor.White));
            for (int c = 0; c < 8; c++)
                SetPiece(6, c, new Piece(PieceType.Pawn, PieceColor.White));

            EnPassantRow = EnPassantCol = -1;
        }

        // ?? clone ?????????????????????????????????????????????????????????
        public Board Clone()
        {
            var b = new Board();
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    b._squares[r, c] = _squares[r, c]?.Clone();
            b.EnPassantRow = EnPassantRow;
            b.EnPassantCol = EnPassantCol;
            return b;
        }

        public void LoadFEN(string fen)
        {
            // Очищаем доску
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    SetPiece(r, c, null);

            string[] parts = fen.Split(' ');
            string[] rows = parts[0].Split('/');

            for (int r = 0; r < 8; r++)
            {
                int col = 0;
                foreach (char ch in rows[r])
                {
                    if (char.IsDigit(ch))
                    {
                        col += ch - '0';
                    }
                    else
                    {
                        var color = char.IsUpper(ch)
                            ? PieceColor.White
                            : PieceColor.Black;

                        var type = char.ToLower(ch) switch
                        {
                            'p' => PieceType.Pawn,
                            'n' => PieceType.Knight,
                            'b' => PieceType.Bishop,
                            'r' => PieceType.Rook,
                            'q' => PieceType.Queen,
                            'k' => PieceType.King,
                            _ => PieceType.Pawn
                        };

                        SetPiece(r, col, new Piece(type, color));
                        col++;
                    }
                }
            }
        }



        // ?? find king ?????????????????????????????????????????????????????
        public (int row, int col) FindKing(PieceColor color)
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var p = _squares[r, c];
                    if (p != null && p.Type == PieceType.King && p.Color == color)
                        return (r, c);
                }
            return (-1, -1);
        }

        // ?? in bounds ?????????????????????????????????????????????????????
        public static bool InBounds(int row, int col) =>
            row >= 0 && row < 8 && col >= 0 && col < 8;

        // ?? apply move (low-level, no validation) ?????????????????????????
        public void ApplyMoveLowLevel(Move move)
        {
            var piece = _squares[move.FromRow, move.FromCol];
            if (piece == null) return;

            // en passant capture
            if (move.IsEnPassant)
            {
                _squares[move.ToRow, move.ToCol] = piece;
                _squares[move.FromRow, move.FromCol] = null;
                // Remove captured pawn
                int captureRow = piece.Color == PieceColor.White ? move.ToRow + 1 : move.ToRow - 1;
                _squares[captureRow, move.ToCol] = null;
            }
            else if (move.IsCastling)
            {
                // Move king
                _squares[move.ToRow, move.ToCol] = piece;
                _squares[move.FromRow, move.FromCol] = null;
                // Move rook
                var rook = _squares[move.FromRow, move.CastlingRookFromCol];
                _squares[move.FromRow, move.CastlingRookFromCol] = null;
                _squares[move.FromRow, move.CastlingRookToCol] = rook;
                if (rook != null) rook.HasMoved = true;
            }
            else
            {
                _squares[move.ToRow, move.ToCol] = piece;
                _squares[move.FromRow, move.FromCol] = null;
            }

            // promotion
            if (move.IsPromotion)
                _squares[move.ToRow, move.ToCol] = new Piece(move.PromotionPiece, piece.Color) { HasMoved = true };

            piece.HasMoved = true;

            // Update en passant target
            if (piece.Type == PieceType.Pawn && Math.Abs(move.ToRow - move.FromRow) == 2)
            {
                EnPassantRow = (move.FromRow + move.ToRow) / 2;
                EnPassantCol = move.FromCol;
            }
            else
            {
                EnPassantRow = EnPassantCol = -1;
            }
        }
    }
}
