using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using ChessWPF.Models;

namespace ChessWPF.Views
{
    public partial class ProfileView : Page
    {
        private readonly PlayerProfile _profile = PlayerProfile.Instance;

        public ProfileView()
        {
            InitializeComponent();
            LoadProfile();
            BuildSkinButtons();
            DrawBoardPreview();
        }

        // ── загрузка данных ───────────────────────────────────────────────
        private void LoadProfile()
        {
            TxtNickname.Text = _profile.Nickname;
            TxtRankIcon.Text = _profile.RankIcon;
            TxtRank.Text = _profile.Rank;
            TxtRegistered.Text = $"В игре с {_profile.RegisteredAt:dd.MM.yyyy}";
            TxtCurrentStreak.Text = $"{_profile.CurrentStreak} побед подряд";

            TxtWins.Text = _profile.Wins.ToString();
            TxtLosses.Text = _profile.Losses.ToString();
            TxtDraws.Text = _profile.Draws.ToString();
            TxtWinRate.Text = $"{_profile.WinRate}%";
            TxtCheckmates.Text = _profile.Checkmates.ToString();
            TxtStalemates.Text = _profile.Stalemates.ToString();
            TxtGamesPlayed.Text = _profile.GamesPlayed.ToString();
            TxtTotalMoves.Text = _profile.TotalMovesPlayed.ToString();
            TxtBestStreak.Text = $"{_profile.BestStreak} 🔥";

            // Аватарка
            LoadAvatar();

            // Прогресс-бар
            Loaded += (s, e) =>
            {
                var container = WinRateBar.Parent as FrameworkElement;
                if (container != null)
                    WinRateBar.Width = container.ActualWidth * _profile.WinRate / 100.0;
            };
        }

        private void LoadAvatar()
        {
            if (!string.IsNullOrEmpty(_profile.AvatarPath) &&
                File.Exists(_profile.AvatarPath))
            {
                try
                {
                    var bmp = new BitmapImage(new Uri(_profile.AvatarPath));
                    ImgAvatar.Source = bmp;
                    ImgAvatar.Visibility = Visibility.Visible;
                    TxtAvatarIcon.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    ImgAvatar.Visibility = Visibility.Collapsed;
                    TxtAvatarIcon.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ImgAvatar.Visibility = Visibility.Collapsed;
                TxtAvatarIcon.Visibility = Visibility.Visible;
            }
        }

        // ── смена аватарки ────────────────────────────────────────────────
        private void ChangeAvatar_Click(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите аватарку",
                Filter = "Изображения (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dlg.ShowDialog() != true) return;

            // Копируем файл в папку приложения
            string appDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChessWPF");
            Directory.CreateDirectory(appDir);

            string ext = System.IO.Path.GetExtension(dlg.FileName);
            string destPath = System.IO.Path.Combine(appDir, $"avatar{ext}");

            File.Copy(dlg.FileName, destPath, overwrite: true);

            _profile.AvatarPath = destPath;
            _profile.Save();
            LoadAvatar();
        }

        // ── редактирование ника ───────────────────────────────────────────
        private void EditNickname_Click(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            EditPanel.Visibility = EditPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;
            TxtNewNick.Text = _profile.Nickname;
            TxtNewNick.Focus();
        }

        private void SaveNickname_Click(object sender, RoutedEventArgs e)
        {
            string newNick = TxtNewNick.Text.Trim();
            if (string.IsNullOrEmpty(newNick)) return;

            _profile.Nickname = newNick;
            _profile.Save();
            TxtNickname.Text = newNick;
            EditPanel.Visibility = Visibility.Collapsed;
        }

        // ── скины доски ───────────────────────────────────────────────────
        private void BuildSkinButtons()
        {
            SkinButtons.Children.Clear();

            foreach (var skin in PlayerProfile.BoardSkins)
            {
                var btn = new Border
                {
                    Width = 56,
                    Height = 56,
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(5),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    BorderThickness = new Thickness(3),
                    BorderBrush = skin.Name == _profile.BoardSkin
                                    ? new SolidColorBrush(Color.FromRgb(233, 69, 96))
                                    : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    ToolTip = skin.Name
                };

                // Мини-превью шахматной клетки
                var grid = new Grid();
                var lightColor = (Color)ColorConverter.ConvertFromString(skin.Light);
                var darkColor = (Color)ColorConverter.ConvertFromString(skin.Dark);

                for (int r = 0; r < 4; r++)
                    for (int c = 0; c < 4; c++)
                    {
                        var rect = new Rectangle
                        {
                            Width = 14,
                            Height = 14,
                            Fill = (r + c) % 2 == 0
                                     ? new SolidColorBrush(lightColor)
                                     : new SolidColorBrush(darkColor)
                        };
                        Canvas.SetLeft(rect, c * 14);
                        Canvas.SetTop(rect, r * 14);
                        grid.Children.Add(rect);
                    }

                // Название скина внизу
                var nameLabel = new TextBlock
                {
                    Text = skin.Name,
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 176)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    FontFamily = new FontFamily("Segoe UI"),
                    Margin = new Thickness(0, 0, 0, 2)
                };

                var canvas = new Canvas { Width = 56, Height = 56 };
                var miniBoard = new Canvas { Width = 56, Height = 56 };

                for (int r = 0; r < 4; r++)
                    for (int c = 0; c < 4; c++)
                    {
                        var rect = new Rectangle
                        {
                            Width = 14,
                            Height = 14,
                            Fill = (r + c) % 2 == 0
                                     ? new SolidColorBrush(lightColor)
                                     : new SolidColorBrush(darkColor)
                        };
                        Canvas.SetLeft(rect, c * 14);
                        Canvas.SetTop(rect, r * 14);
                        miniBoard.Children.Add(rect);
                    }

                canvas.Children.Add(miniBoard);

                var label = new TextBlock
                {
                    Text = skin.Name,
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Colors.White),
                    FontFamily = new FontFamily("Segoe UI"),
                    Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    Padding = new Thickness(2, 1, 2, 1),
                    Width = 56,
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetTop(label, 38);
                canvas.Children.Add(label);

                btn.Child = canvas;

                string skinName = skin.Name;
                btn.MouseLeftButtonDown += (s, e) =>
                {
                    _profile.BoardSkin = skinName;
                    _profile.Save();
                    BuildSkinButtons();
                    DrawBoardPreview();
                };

                SkinButtons.Children.Add(btn);
            }
        }

        private void DrawBoardPreview()
        {
            BoardPreview.Children.Clear();
            var colors = _profile.GetBoardColors();
            var lightColor = (Color)ColorConverter.ConvertFromString(colors.Light);
            var darkColor = (Color)ColorConverter.ConvertFromString(colors.Dark);
            double cell = 20.0;

            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var rect = new Rectangle
                    {
                        Width = cell,
                        Height = cell,
                        Fill = (r + c) % 2 == 0
                                 ? new SolidColorBrush(lightColor)
                                 : new SolidColorBrush(darkColor)
                    };
                    Canvas.SetLeft(rect, c * cell);
                    Canvas.SetTop(rect, r * cell);
                    BoardPreview.Children.Add(rect);
                }

            // Фигуры на превью
            string[,] pieces =
            {
                {"♜","♞","♝","♛","♚","♝","♞","♜"},
                {"♟","♟","♟","♟","♟","♟","♟","♟"},
                {"","","","","","","",""},
                {"","","","","","","",""},
                {"","","","","","","",""},
                {"","","","","","","",""},
                {"♙","♙","♙","♙","♙","♙","♙","♙"},
                {"♖","♘","♗","♕","♔","♗","♘","♖"}
            };

            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    if (string.IsNullOrEmpty(pieces[r, c])) continue;
                    var txt = new TextBlock
                    {
                        Text = pieces[r, c],
                        FontSize = 13,
                        Width = cell,
                        Height = cell,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = r < 2
                            ? new SolidColorBrush(Colors.Black)
                            : new SolidColorBrush(Colors.White),
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(txt, c * cell);
                    Canvas.SetTop(txt, r * cell + 1);
                    BoardPreview.Children.Add(txt);
                }
        }

        // ── сброс статистики ──────────────────────────────────────────────
        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены? Вся статистика будет удалена безвозвратно.",
                "Сброс статистики",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            string savedNick = _profile.Nickname;
            string savedSkin = _profile.BoardSkin;
            string savedAvatar = _profile.AvatarPath;

            _profile.GamesPlayed = 0;
            _profile.Wins = 0;
            _profile.Losses = 0;
            _profile.Draws = 0;
            _profile.Checkmates = 0;
            _profile.Stalemates = 0;
            _profile.BestStreak = 0;
            _profile.CurrentStreak = 0;
            _profile.TotalMovesPlayed = 0;
            _profile.Nickname = savedNick;
            _profile.BoardSkin = savedSkin;
            _profile.AvatarPath = savedAvatar;
            _profile.Save();

            LoadProfile();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.NavigateToMainMenu();
        }
    }
}
