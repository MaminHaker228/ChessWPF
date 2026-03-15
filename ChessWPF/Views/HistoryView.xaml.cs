using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChessWPF.Models;

namespace ChessWPF.Views
{
    public partial class HistoryView : Page
    {
        public HistoryView()
        {
            InitializeComponent();
            BuildList();
        }

        private void BuildList()
        {
            GameList.Items.Clear();
            var records = GameHistory.Records;

            if (records.Count == 0)
            {
                GameList.Items.Add(new TextBlock
                {
                    Text = "Нет сыгранных партий",
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 176)),
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 15,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];

                // Цвет результата
                Color resultColor = r.Result switch
                {
                    "Победа" => Color.FromRgb(39, 174, 96),
                    "Поражение" => Color.FromRgb(233, 69, 96),
                    _ => Color.FromRgb(243, 156, 18)
                };

                string icon = r.Result switch
                {
                    "Победа" => "🏆",
                    "Поражение" => "💀",
                    _ => "🤝"
                };

                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(22, 33, 62)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 0, 10),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Иконка
                var iconTxt = new TextBlock
                {
                    Text = icon,
                    FontSize = 28,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(iconTxt, 0);

                // Инфо
                var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                info.Children.Add(new TextBlock
                {
                    Text = $"{r.Result} — {r.EndReason}",
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(resultColor),
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
                });
                info.Children.Add(new TextBlock
                {
                    Text = $"{r.Date:dd.MM.yyyy  HH:mm}  •  {r.MovesCount} ходов  •  {r.Mode}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 176)),
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
                });
                Grid.SetColumn(info, 1);

                // Кнопка анализа
                var analyzeBtn = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(42, 42, 74)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 6, 10, 6),
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                analyzeBtn.Child = new TextBlock
                {
                    Text = "🔍 Анализ",
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 176)),
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 12
                };
                Grid.SetColumn(analyzeBtn, 2);

                var record = r;
                analyzeBtn.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    MainWindow.Instance.NavigateToAnalysis(record);
                };

                grid.Children.Add(iconTxt);
                grid.Children.Add(info);
                grid.Children.Add(analyzeBtn);
                card.Child = grid;

                GameList.Items.Add(card);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.NavigateToMainMenu();
        }
    }
}
