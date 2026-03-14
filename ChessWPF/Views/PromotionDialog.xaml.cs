using System.Windows;
using ChessWPF.Models;

namespace ChessWPF.Views
{
    public partial class PromotionDialog : Window
    {
        public PieceType ChosenPiece { get; private set; } = PieceType.Queen;
        private PieceColor _color;

        public PromotionDialog(PieceColor color)
        {
            InitializeComponent();
            _color = color;
            // Update symbols for white
            if (color == PieceColor.White)
            {
                BtnQueen.Content = "♛ Ферзь";
                BtnRook.Content = "♖ Ладья";
                BtnBishop.Content = "♗ Слон";
                BtnKnight.Content = "♘ Конь";
            }
        }

        private void BtnQueen_Click(object sender, RoutedEventArgs e) { ChosenPiece = PieceType.Queen; Close(); }
        private void BtnRook_Click(object sender, RoutedEventArgs e) { ChosenPiece = PieceType.Rook; Close(); }
        private void BtnBishop_Click(object sender, RoutedEventArgs e) { ChosenPiece = PieceType.Bishop; Close(); }
        private void BtnKnight_Click(object sender, RoutedEventArgs e) { ChosenPiece = PieceType.Knight; Close(); }
    }
}
