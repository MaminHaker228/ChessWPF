using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ChessWPF.Models;
using ChessWPF.GameLogic;
using ChessWPF.UI;

namespace ChessWPF.Views
{
    public partial class PuzzleView : Page
    {
        private const double BoardSize = 480.0;
        private const double CellSize  = BoardSize / 8.0;

        private readonly Rectangle[,] _tiles     = new Rectangle[8, 8];
        private readonly TextBlock[,]  _pieceText = new TextBlock[8, 8];

        private int     _puzzleIndex = 0;
        private Puzzle  _current;
        private bool    _solved = false;

        private int  _selRow = -1, _selCol = -1;
        private List<Move> _legalMoves = new();

        // Упрощённая доска только для отображения головоломок
        private Board _board;

        public PuzzleView()
        {
            InitializeComponent();
            BuildBoardVisuals();
            SetupBoardLabels();
            LoadPuzzle(_puzzleIndex);
        }

        // ── построение доски ──────────────────────────────────────────────
        private void BuildBoardVisuals()
        {
            BoardCanvas.Children.Clear();
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var tile = new Rectangle
                    {
                        Width = CellSize, Height = CellSize,
                        Fill  = GetTileColor(r, c)
                    };
                    Canvas.SetLeft(tile, c * CellSize);
                    Canvas.SetTop(tile,  r * CellSize);
                    BoardCanvas.Children.Add(tile);
                    _tiles[r, c] = tile;

                    var txt = new TextBlock
                    {
                        Width    = CellSize, Height = CellSize,
                        FontSize = CellSize * 0.62,
                        TextAlignment    = TextAlignment.Center,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(txt, c * CellSize);
                    Canvas.SetTop(txt,  r * CellSize + CellSize * 0.05);
                    BoardCanvas.Children.Add(txt);
                    _pieceText[r, c] = txt;
                }
        }

        private SolidColorBrush GetTileColor(int row, int col)
        {
            bool light  = (row + col) % 2 == 0;
            var  colors = PlayerProfile.Instance.GetBoardColors();
            var  lc     = (Color)ColorConverter.ConvertFromString(colors.Light);
            var  dc     = (Color)ColorConverter.ConvertFromString(colors.Dark);
            return light ? new SolidColorBrush(lc) : new SolidColorBrush(dc);
        }

        private void SetupBoardLabels()
        {
            var fp = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var f in new[] { "a","b","c","d","e","f","g","h" })
                fp.Children.Add(new TextBlock
                {
                    Text = f, Width = CellSize,
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(160,160,176)),
                    FontFamily = new FontFamily("Segoe UI"), FontSize = 12
                });
            FileLabels.Items.Clear();
            FileLabels.Items.Add(fp);

            RankLabels.Items.Clear();
            for (int r = 0; r < 8; r++)
                RankLabels.Items.Add(new TextBlock
                {
                    Text = (8-r).ToString(), Height = CellSize, Width = 18,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment     = TextAlignment.Right,
                    Foreground = new SolidColorBrush(Color.FromRgb(160,160,176)),
                    FontFamily = new FontFamily("Segoe UI"), FontSize = 12,
                    Margin = new Thickness(0,0,4,0)
                });
        }

        // ── загрузка головоломки ──────────────────────────────────────────
        private void LoadPuzzle(int index)
        {
            _current = PuzzleData.All[index];
            _solved  = false;
            _selRow  = _selCol = -1;
            _legalMoves.Clear();

            TxtPuzzleTitle.Text  = _current.Title;
            TxtPuzzleDesc.Text   = _current.Description;
            TxtDifficulty.Text   = _current.Difficulty;
            TxtProgress.Text     = $"{index + 1} / {PuzzleData.All.Count}";
            TxtStatus.Text       = "Белые начинают. Найди лучший ход!";
            TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(160,160,176));

            _board = new Board();
            _board.LoadFEN(_current.FEN);

            Render();
        }

        private void Render()
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    _tiles[r,c].Fill = GetTileColor(r, c);

            if (_selRow >= 0)
            {
                _tiles[_selRow, _selCol].Fill =
                    new SolidColorBrush(Color.FromArgb(200, 100, 200, 100));
                foreach (var mv in _legalMoves)
                    _tiles[mv.ToRow, mv.ToCol].Fill =
                        new SolidColorBrush(Color.FromArgb(180, 60, 170, 230));
            }

            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var piece = _board.GetPiece(r, c);
                    _pieceText[r,c].Text = piece != null
                        ? PieceImages.GetUnicode(piece) : "";
                    _pieceText[r,c].Foreground = piece?.Color == PieceColor.White
                        ? Brushes.White : Brushes.Black;
                    _pieceText[r,c].Effect = piece?.Color == PieceColor.White
                        ? new System.Windows.Media.Effects.DropShadowEffect
                          { Color = Colors.Black, ShadowDepth = 1, BlurRadius = 3 }
                        : null;
                }
        }

        // ── клики ─────────────────────────────────────────────────────────
        private void BoardCanvas_Click(object sender, MouseButtonEventArgs e)
        {
            if (_solved) return;

            var pos = e.GetPosition(BoardCanvas);
            int col = (int)(pos.X / CellSize);
            int row = (int)(pos.Y / CellSize);
            if (row < 0 || row > 7 || col < 0 || col > 7) return;

            var piece = _board.GetPiece(row, col);

            if (_selRow >= 0)
            {
                // Проверяем совпадает ли ход с решением
                string moveStr = ColToChar(_selCol) + (8 - _selRow).ToString()
                               + ColToChar(col)     + (8 - row).ToString();

                if (moveStr == _current.Solution)
                {
                    // Правильный ход!
                    _board.ApplyMoveLowLevel(
                        new Move(_selRow, _selCol, row, col));
                    _selRow = _selCol = -1;
                    _legalMoves.Clear();
                    _solved = true;
                    Render();
                    TxtStatus.Text = "✅ Правильно! Отличный ход!";
                    TxtStatus.Foreground =
                        new SolidColorBrush(Color.FromRgb(39, 174, 96));
                    return;
                }

                // Неправильный ход
                if (piece != null && piece.Color == PieceColor.White)
                {
                    _selRow = row; _selCol = col;
                    _legalMoves = MoveValidator.GetLegalMoves(_board, row, col);
                    Render();
                    return;
                }

                _selRow = _selCol = -1;
                _legalMoves.Clear();
                TxtStatus.Text = "❌ Неверно, попробуй ещё раз";
                TxtStatus.Foreground =
                    new SolidColorBrush(Color.FromRgb(233, 69, 96));
                Render();
                return;
            }

            if (piece != null && piece.Color == PieceColor.White)
            {
                _selRow = row; _selCol = col;
                _legalMoves = MoveValidator.GetLegalMoves(_board, row, col);
                TxtStatus.Text = "Выбери куда пойти";
                TxtStatus.Foreground =
                    new SolidColorBrush(Color.FromRgb(160,160,176));
            }

            Render();
        }

        private string ColToChar(int col) =>
            ((char)('a' + col)).ToString();

        // ── кнопки ────────────────────────────────────────────────────────
        private void BtnHint_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = $"💡 Подсказка: {_current.Hint}";
            TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(243,156,18));

            // Подсвечиваем фигуру которую нужно двигать
            int fromCol = _current.Solution[0] - 'a';
            int fromRow = 8 - int.Parse(_current.Solution[1].ToString());
            _tiles[fromRow, fromCol].Fill =
                new SolidColorBrush(Color.FromArgb(200, 243, 156, 18));
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            _puzzleIndex = (_puzzleIndex + 1) % PuzzleData.All.Count;
            LoadPuzzle(_puzzleIndex);
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            LoadPuzzle(_puzzleIndex);
        }

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.NavigateToMainMenu();
        }
    }
}
