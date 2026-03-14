// BoardRenderer.cs — helper utilities for board coordinate mapping.
// Core rendering is handled inline in GameView.xaml.cs using Canvas + TextBlock.
// This class provides reusable geometry helpers.

using System.Windows;

namespace ChessWPF.UI
{
    public static class BoardRenderer
    {
        public const double BoardSize = 560.0;
        public const double CellSize = BoardSize / 8.0;

        public static Point SquareToCanvas(int row, int col)
            => new Point(col * CellSize, row * CellSize);

        public static (int row, int col) CanvasToSquare(double x, double y)
            => ((int)(y / CellSize), (int)(x / CellSize));
    }
}
