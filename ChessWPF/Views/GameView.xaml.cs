using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ChessWPF.GameLogic;
using ChessWPF.Models;
using ChessWPF.AI;
using ChessWPF.Network;
using ChessWPF.UI;

namespace ChessWPF.Views
{
    public partial class GameView : Page
    {
        // ── layout constants ──────────────────────────────────────────────
        private const double BoardSize = 560.0;
        private const double CellSize = BoardSize / 8.0;

        // ── core state ────────────────────────────────────────────────────
        private GameManager _game;
        private GameMode _mode;
        private string _networkAddress;
        private bool _isAnimating;

        // selected square
        private int _selRow = -1, _selCol = -1;
        private List<Move> _legalMovesForSelected = new();

        // WPF visuals per square
        private readonly Rectangle[,] _tiles = new Rectangle[8, 8];
        private readonly TextBlock[,] _pieceText = new TextBlock[8, 8];

        // network
        private LANServer _server;
        private LANClient _client;
        private bool _isMyTurn = true;   // for LAN
        private PieceColor _myColor = PieceColor.White;

        // ── constructor ───────────────────────────────────────────────────
        public GameView(GameMode mode, string networkAddress = null)
        {
            InitializeComponent();
            _mode = mode;
            _networkAddress = networkAddress;

            _game = new GameManager();
            _game.OnGameOver += HandleGameOver;
            _game.OnCheck += HandleCheck;

            BuildBoardVisuals();
            SetupBoardLabels();
            Render();

            switch (mode)
            {
                case GameMode.VsAI:
                    TxtNetInfo.Text = "Режим: Игра против ИИ";
                    _isMyTurn = true;
                    break;
                case GameMode.LanHost:
                    StartAsHost();
                    break;
                case GameMode.LanClient:
                    StartAsClient();
                    break;
            }
        }

        // ── board construction ────────────────────────────────────────────
        private void BuildBoardVisuals()
        {
            BoardCanvas.Children.Clear();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var tile = new Rectangle
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Fill = GetTileColor(row, col)
                    };
                    Canvas.SetLeft(tile, col * CellSize);
                    Canvas.SetTop(tile, row * CellSize);
                    BoardCanvas.Children.Add(tile);
                    _tiles[row, col] = tile;

                    var txt = new TextBlock
                    {
                        Width = CellSize,
                        Height = CellSize,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = CellSize * 0.62,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(txt, col * CellSize);
                    Canvas.SetTop(txt, row * CellSize + CellSize * 0.05);
                    BoardCanvas.Children.Add(txt);
                    _pieceText[row, col] = txt;
                }
            }
        }

        private SolidColorBrush GetTileColor(int row, int col)
        {
            bool light = (row + col) % 2 == 0;
            return light
                ? new SolidColorBrush(Color.FromRgb(238, 216, 181))
                : new SolidColorBrush(Color.FromRgb(181, 136, 99));
        }

        private void SetupBoardLabels()
        {
            // file labels (a-h) above board
            var filePanel = new StackPanel { Orientation = Orientation.Horizontal };
            string[] files = { "a", "b", "c", "d", "e", "f", "g", "h" };
            foreach (var f in files)
            {
                filePanel.Children.Add(new TextBlock
                {
                    Text = f,
                    Width = CellSize,
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 176)),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 13
                });
            }
            FileLabels.Items.Clear();
            FileLabels.Items.Add(filePanel);

            // rank labels (8-1)
            RankLabels.Items.Clear();
            for (int r = 0; r < 8; r++)
            {
                RankLabels.Items.Add(new TextBlock
                {
                    Text = (8 - r).ToString(),
                    Height = CellSize,
                    Width = 18,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Right,
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 176)),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 4, 0)
                });
            }
        }

        // ── rendering ─────────────────────────────────────────────────────
        private void Render()
        {
            // reset tile colors
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    _tiles[r, c].Fill = GetTileColor(r, c);

            // highlight selected
            if (_selRow >= 0)
            {
                _tiles[_selRow, _selCol].Fill =
                    new SolidColorBrush(Color.FromArgb(200, 100, 200, 100));

                foreach (var mv in _legalMovesForSelected)
                    _tiles[mv.ToRow, mv.ToCol].Fill =
                        new SolidColorBrush(Color.FromArgb(180, 60, 170, 230));
            }

            // last move highlight
            if (_game.LastMove != null)
            {
                var lm = _game.LastMove;
                _tiles[lm.FromRow, lm.FromCol].Fill =
                    new SolidColorBrush(Color.FromArgb(120, 255, 220, 60));
                _tiles[lm.ToRow, lm.ToCol].Fill =
                    new SolidColorBrush(Color.FromArgb(150, 255, 220, 60));
            }

            // draw pieces
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var piece = _game.Board.GetPiece(r, c);
                    _pieceText[r, c].Text = piece != null ? PieceImages.GetUnicode(piece) : "";
                    _pieceText[r, c].Foreground = piece != null && piece.Color == PieceColor.White
                        ? Brushes.White
                        : Brushes.Black;
                    // drop shadow for white pieces so they show on light squares
                    if (piece != null && piece.Color == PieceColor.White)
                    {
                        _pieceText[r, c].Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = Colors.Black,
                            ShadowDepth = 1,
                            BlurRadius = 3
                        };
                    }
                    else
                    {
                        _pieceText[r, c].Effect = null;
                    }
                }
            }

            // update side panel
            UpdateSidePanel();
        }

        private void UpdateSidePanel()
        {
            TxtStatus.Text = _game.IsGameOver ? GetGameOverText()
                           : _game.CurrentTurn == PieceColor.White ? "Ваш ход (Белые)"
                                                                    : "Ход соперника (Чёрные)";

            if (_game.IsCheck && !_game.IsGameOver)
                TxtStatus.Text = "⚠ ШАХ!";

            TxtCapturedBlack.Text = PieceImages.CapturedString(_game.CapturedByWhite);
            TxtCapturedWhite.Text = PieceImages.CapturedString(_game.CapturedByBlack);
            TxtMoveHistory.Text = _game.MoveHistoryText;
        }

        private string GetGameOverText()
        {
            if (_game.IsCheckmate)
            {
                var winner = _game.CurrentTurn == PieceColor.White ? "Чёрные" : "Белые";
                return $"МАТ! Победа — {winner}";
            }
            if (_game.IsStalemate) return "ПАТ — Ничья";
            return "Игра окончена";
        }

        // ── click handling ────────────────────────────────────────────────
        private void BoardCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAnimating || _game.IsGameOver) return;
            if (_mode != GameMode.VsAI && !_isMyTurn) return;
            if (_mode == GameMode.VsAI && _game.CurrentTurn == PieceColor.Black) return;

            var pos = e.GetPosition(BoardCanvas);
            int col = (int)(pos.X / CellSize);
            int row = (int)(pos.Y / CellSize);
            if (row < 0 || row > 7 || col < 0 || col > 7) return;

            HandleSquareClick(row, col);
        }

        private void HandleSquareClick(int row, int col)
        {
            var piece = _game.Board.GetPiece(row, col);

            // if a piece is already selected, try to move
            if (_selRow >= 0)
            {
                var move = _legalMovesForSelected.Find(m => m.ToRow == row && m.ToCol == col);
                if (move != null)
                {
                    // pawn promotion check
                    if (move.IsPromotion)
                    {
                        var promo = ShowPromotionDialog(_game.CurrentTurn);
                        move = new Move(move.FromRow, move.FromCol, move.ToRow, move.ToCol,
                                        isPromotion: true, promotionPiece: promo);
                    }
                    ExecutePlayerMove(move);
                    return;
                }
                // deselect
                _selRow = _selCol = -1;
                _legalMovesForSelected.Clear();
            }

            // select new piece
            if (piece != null && piece.Color == _game.CurrentTurn)
            {
                _selRow = row;
                _selCol = col;
                _legalMovesForSelected = _game.GetLegalMoves(row, col);
            }

            Render();
        }

        private void ExecutePlayerMove(Move move)
        {
            _selRow = _selCol = -1;
            _legalMovesForSelected.Clear();

            AnimateMove(move, () =>
            {
                _game.ApplyMove(move);
                Render();

                if (_mode == GameMode.LanHost || _mode == GameMode.LanClient)
                {
                    SendNetworkMove(move);
                    _isMyTurn = false;
                    TxtStatus.Text = "Ход соперника";
                }
                else if (_mode == GameMode.VsAI && !_game.IsGameOver)
                {
                    TriggerAIMove();
                }
            });
        }

        // ── animation ─────────────────────────────────────────────────────
        private void AnimateMove(Move move, Action onComplete)
        {
            _isAnimating = true;

            var piece = _game.Board.GetPiece(move.FromRow, move.FromCol);
            if (piece == null) { _isAnimating = false; onComplete?.Invoke(); return; }

            // ghost piece for animation
            var ghost = new TextBlock
            {
                Text = PieceImages.GetUnicode(piece),
                FontSize = CellSize * 0.62,
                Foreground = piece.Color == PieceColor.White ? Brushes.White : Brushes.Black,
                IsHitTestVisible = false
            };
            if (piece.Color == PieceColor.White)
                ghost.Effect = new System.Windows.Media.Effects.DropShadowEffect
                { Color = Colors.Black, ShadowDepth = 1, BlurRadius = 3 };

            double fromX = move.FromCol * CellSize;
            double fromY = move.FromRow * CellSize + CellSize * 0.05;
            double toX = move.ToCol * CellSize;
            double toY = move.ToRow * CellSize + CellSize * 0.05;

            Canvas.SetLeft(ghost, fromX);
            Canvas.SetTop(ghost, fromY);
            BoardCanvas.Children.Add(ghost);

            // hide original
            _pieceText[move.FromRow, move.FromCol].Text = "";

            var da = new DoubleAnimation(fromX, toX, new Duration(TimeSpan.FromMilliseconds(200)));
            var da2 = new DoubleAnimation(fromY, toY, new Duration(TimeSpan.FromMilliseconds(200)));
            da.Completed += (s, e) =>
            {
                BoardCanvas.Children.Remove(ghost);
                _isAnimating = false;
                onComplete?.Invoke();
            };

            ghost.BeginAnimation(Canvas.LeftProperty, da);
            ghost.BeginAnimation(Canvas.TopProperty, da2);
        }

        // ── AI ────────────────────────────────────────────────────────────
        private void TriggerAIMove()
        {
            TxtStatus.Text = "ИИ думает...";
            System.Threading.Tasks.Task.Run(() =>
            {
                var aiMove = AIManager.GetBestMove(_game.Board, PieceColor.Black, depth: 3);
                Dispatcher.Invoke(() =>
                {
                    if (aiMove == null || _game.IsGameOver) return;
                    AnimateMove(aiMove, () =>
                    {
                        _game.ApplyMove(aiMove);
                        Render();
                    });
                });
            });
        }

        // ── pawn promotion dialog ─────────────────────────────────────────
        private PieceType ShowPromotionDialog(PieceColor color)
        {
            var dlg = new PromotionDialog(color);
            dlg.Owner = MainWindow.Instance;
            dlg.ShowDialog();
            return dlg.ChosenPiece;
        }

        // ── game over ─────────────────────────────────────────────────────
        private void HandleGameOver(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TxtStatus.Text = message;
                var dlg = new GameOverDialog(message);
                dlg.Owner = MainWindow.Instance;
                if (dlg.ShowDialog() == true)
                    RestartGame();
                else
                    MainWindow.Instance.NavigateToMainMenu();
            });
        }

        private void HandleCheck()
        {
            Dispatcher.Invoke(() => TxtStatus.Text = "⚠ ШАХ!");
        }

        private void RestartGame()
        {
            _game = new GameManager();
            _game.OnGameOver += HandleGameOver;
            _game.OnCheck += HandleCheck;
            _selRow = _selCol = -1;
            _legalMovesForSelected.Clear();
            _isMyTurn = _mode != GameMode.LanClient;
            Render();
        }

        // ── network ───────────────────────────────────────────────────────
        private void StartAsHost()
        {
            _myColor = PieceColor.White;
            _isMyTurn = true;
            _server = new LANServer();
            _server.OnMoveReceived += OnNetworkMoveReceived;
            _server.OnClientConnected += () =>
                Dispatcher.Invoke(() => TxtNetInfo.Text = "Клиент подключён ✓");
            _server.Start();
            TxtNetInfo.Text = $"Хост • Порт 5555 • Ожидание игрока...";
        }

        private void StartAsClient()
        {
            _myColor = PieceColor.Black;
            _isMyTurn = false;
            _client = new LANClient();
            _client.OnMoveReceived += OnNetworkMoveReceived;
            _client.Connect(_networkAddress, 5555);
            TxtNetInfo.Text = $"Клиент • {_networkAddress}:5555";
            TxtStatus.Text = "Ход соперника";
        }

        private void OnNetworkMoveReceived(Move move)
        {
            Dispatcher.Invoke(() =>
            {
                AnimateMove(move, () =>
                {
                    _game.ApplyMove(move);
                    _isMyTurn = true;
                    Render();
                });
            });
        }

        private void SendNetworkMove(Move move)
        {
            string data = $"{move.FromRow},{move.FromCol},{move.ToRow},{move.ToCol}," +
                          $"{move.IsPromotion},{(int)move.PromotionPiece}," +
                          $"{move.IsEnPassant},{move.IsCastling}";
            if (_mode == GameMode.LanHost) _server?.SendMove(data);
            else if (_mode == GameMode.LanClient) _client?.SendMove(data);
        }

        // ── button handlers ───────────────────────────────────────────────
        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            _server?.Stop();
            _client?.Disconnect();
            MainWindow.Instance.NavigateToMainMenu();
        }

        private void BtnNewGame_Click(object sender, RoutedEventArgs e)
        {
            RestartGame();
        }
    }
}
