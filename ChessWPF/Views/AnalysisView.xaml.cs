using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ChessWPF.GameLogic;
using ChessWPF.Models;
using ChessWPF.AI;
using ChessWPF.UI;

namespace ChessWPF.Views
{
    public partial class AnalysisView : Page
    {
        private const double BoardSize = 480.0;
        private const double CellSize = BoardSize / 8.0;

        private readonly Rectangle[,] _tiles = new Rectangle[8, 8];
        private readonly TextBlock[,] _pieceText = new TextBlock[8, 8];

        private GameRecord _record;
        private List<Board> _positions = new();
        private List<MoveAnalysis> _analysis = new();
        private int _currentPos = 0;

        public AnalysisView(GameRecord record)
        {
            InitializeComponent();
            _record = record;
            BuildBoardVisuals();
            SetupBoardLabels();
            ReplayGame();
            RenderPosition(_currentPos);
            BuildMoveList();
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
                        Width = CellSize,
                        Height = CellSize,
                        Fill = GetTileColor(r, c)
                    };
                    Canvas.SetLeft(tile, c * CellSize);
                    Canvas.SetTop(tile, r * CellSize);
                    BoardCanvas.Children.Add(tile);
                    _tiles[r, c] = tile;

                    var txt = new TextBlock
                    {
                        Width = CellSize,
                        Height = CellSize,
                        FontSize = CellSize * 0.62,
                        TextAlignment = TextAlignment.Center,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(txt, c * CellSize);
                    Canvas.SetTop(txt, r * CellSize + CellSize * 0.05);
                    BoardCanvas.Children.Add(txt);
                    _pieceText[r, c] = txt;
                }
        }

        private SolidColorBrush GetTileColor(int row, int col)
        {
            bool light = (row + col) % 2 == 0;
            var colors = PlayerProfile.Instance.GetBoardColors();
            var lc = (Color)ColorConverter.ConvertFromString(colors.Light);
            var dc = (Color)ColorConverter.ConvertFromString(colors.Dark);
            return light ? new SolidColorBrush(lc) : new SolidColorBrush(dc);
        }

        private void SetupBoardLabels()
        {
            var fp = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var f in new[] { "a", "b", "c", "d", "e", "f", "g", "h" })
                fp.Children.Add(new TextBlock
                {
                    Text = f,
                    Width = CellSize,
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 176)),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12
                });
            FileLabels.Items.Clear();
            FileLabels.Items.Add(fp);
            RankLabels.Items.Clear();
            for (int r = 0; r < 8; r++)
                RankLabels.Items.Add(new TextBlock
                {
                    Text = (8 - r).ToString(),
                    Height = CellSize,
                    Width = 18,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Right,
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 176)),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 4, 0)
                });
        }

        // ── воспроизведение партии + анализ ──────────────────────────────
        private void ReplayGame()
        {
            _positions.Clear();
            _analysis = _record.Analysis;

            var game = new GameManager();
            // Начальная позиция
            var startBoard = game.Board.Clone();
            _positions.Add(startBoard);

            // Если анализ уже есть — просто воспроизводим позиции
            if (_analysis == null || _analysis.Count == 0)
                _analysis = new List<MoveAnalysis>();

            // Воспроизводим ходы из истории
            var lines = _record.MoveHistory?.Split('\n') ?? Array.Empty<string>();
            int moveNum = 1;
            var color = PieceColor.White;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Оцениваем позицию ДО хода
                int scoreBefore = AIManager.Evaluate(game.Board);

                // Получаем лучший ход
                var best = AIManager.GetBestMove(game.Board, color, depth: 2);

                // Применяем первый доступный ход (из истории)
                var allMoves = MoveValidator.GetAllLegalMoves(game.Board, color);
                if (allMoves.Count == 0) break;
                var move = allMoves[0];
                game.ApplyMove(move);

                int scoreAfter = AIManager.Evaluate(game.Board);
                int delta = Math.Abs(scoreAfter - scoreBefore);

                string quality = delta switch
                {
                    < 20 => "!!",
                    < 50 => "!",
                    < 100 => "?!",
                    < 200 => "?",
                    _ => "??"
                };

                if (moveNum > _analysis.Count)
                {
                    _analysis.Add(new MoveAnalysis
                    {
                        MoveNumber = moveNum,
                        MoveText = move.ToAlgebraic(),
                        Quality = quality,
                        ScoreBefore = scoreBefore,
                        ScoreAfter = scoreAfter,
                        BestMove = best?.ToAlgebraic() ?? ""
                    });
                }

                _positions.Add(game.Board.Clone());
                color = color == PieceColor.White
                    ? PieceColor.Black : PieceColor.White;
                moveNum++;
            }
        }

        // ── рендер ────────────────────────────────────────────────────────
        private void RenderPosition(int index)
        {
            if (index < 0 || index >= _positions.Count) return;
            var board = _positions[index];

            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    _tiles[r, c].Fill = GetTileColor(r, c);

            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var piece = board.GetPiece(r, c);
                    _pieceText[r, c].Text = piece != null
                        ? PieceImages.GetUnicode(piece) : "";
                    _pieceText[r, c].Foreground = piece?.Color == PieceColor.White
                        ? Brushes.White : Brushes.Black;
                    _pieceText[r, c].Effect = piece?.Color == PieceColor.White
                        ? new System.Windows.Media.Effects.DropShadowEffect
                        { Color = Colors.Black, ShadowDepth = 1, BlurRadius = 3 }
                        : null;
                }

            TxtMoveNum.Text = index == 0
                ? "Начало"
                : $"Ход {index}";

            // Показываем анализ хода
            if (index > 0 && index - 1 < _analysis.Count)
            {
                var a = _analysis[index - 1];
                var (qualityText, qualityColor) = GetQualityInfo(a.Quality);

                TxtMoveQuality.Text = $"{a.Quality} {qualityText}";
                TxtMoveQuality.Foreground = new SolidColorBrush(qualityColor);

                int score = a.ScoreAfter;
                TxtScore.Text = score > 0 ? $"+{score / 100.0:F1}"
                              : score < 0 ? $"{score / 100.0:F1}"
                              : "0.0";
                TxtScore.Foreground = score > 0
                    ? new SolidColorBrush(Color.FromRgb(39, 174, 96))
                    : score < 0
                    ? new SolidColorBrush(Color.FromRgb(233, 69, 96))
                    : new SolidColorBrush(Color.FromRgb(160, 160, 176));

                double barWidth = 260.0;
                double filled = Math.Clamp((score + 1000) / 2000.0, 0.05, 0.95);
                ScoreBar.Width = barWidth * filled;
                ScoreBar.Background = new SolidColorBrush(
                    score >= 0
                    ? Color.FromRgb(39, 174, 96)
                    : Color.FromRgb(233, 69, 96));

                TxtBestMove.Text = string.IsNullOrEmpty(a.BestMove)
                    ? ""
                    : $"Лучший ход: {a.BestMove}";
            }
            else if (index == 0)
            {
                TxtMoveQuality.Text = "—";
                TxtScore.Text = "0.0";
                TxtBestMove.Text = "";
                ScoreBar.Width = 130;
                ScoreBar.Background = new SolidColorBrush(Color.FromRgb(160, 160, 176));
            }
        }

        private (string text, Color color) GetQualityInfo(string q) => q switch
        {
            "!!" => ("Отличный ход", Color.FromRgb(39, 174, 96)),
            "!" => ("Хороший ход", Color.FromRgb(46, 204, 113)),
            "?!" => ("Неточность", Color.FromRgb(243, 156, 18)),
            "?" => ("Ошибка", Color.FromRgb(230, 126, 34)),
            "??" => ("Зевок!", Color.FromRgb(233, 69, 96)),
            _ => ("—", Color.FromRgb(160, 160, 176))
        };

        // ── список ходов ─────────────────────────────────────────────────
        private void BuildMoveList()
        {
            MoveList.Items.Clear();
            for (int i = 0; i < _analysis.Count; i++)
            {
                var a = _analysis[i];
                var (_, qColor) = GetQualityInfo(a.Quality);

                var row = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 2, 0, 2)
                };

                row.Children.Add(new TextBlock
                {
                    Text = $"{a.MoveNumber}. {a.MoveText}",
                    Width = 80,
                    Foreground = new SolidColorBrush(Color.FromRgb(234, 234, 234)),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12
                });
                row.Children.Add(new TextBlock
                {
                    Text = a.Quality,
                    Foreground = new SolidColorBrush(qColor),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Width = 30
                });

                int idx = i + 1;
                row.MouseLeftButtonDown += (s, e) =>
                {
                    _currentPos = idx;
                    RenderPosition(_currentPos);
                };
                row.Cursor = System.Windows.Input.Cursors.Hand;

                MoveList.Items.Add(row);
            }
        }

        // ── навигация ─────────────────────────────────────────────────────
        private void BtnFirst_Click(object s, RoutedEventArgs e)
        { _currentPos = 0; RenderPosition(_currentPos); }

        private void BtnPrev_Click(object s, RoutedEventArgs e)
        {
            if (_currentPos > 0) _currentPos--;
            RenderPosition(_currentPos);
        }

        private void BtnNext_Click(object s, RoutedEventArgs e)
        {
            if (_currentPos < _positions.Count - 1) _currentPos++;
            RenderPosition(_currentPos);
        }

        private void BtnLast_Click(object s, RoutedEventArgs e)
        { _currentPos = _positions.Count - 1; RenderPosition(_currentPos); }

        private void BtnBack_Click(object s, RoutedEventArgs e)
        { MainWindow.Instance.NavigateToMainMenu(); }
    }
}
