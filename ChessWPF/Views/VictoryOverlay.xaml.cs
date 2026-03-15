using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ChessWPF.Views
{
    public partial class VictoryOverlay : Window
    {
        public bool PlayAgain { get; private set; } = false;

        private readonly DispatcherTimer _confettiTimer = new();
        private readonly Random _rnd = new();
        private readonly List<(Ellipse el, double vx, double vy)> _particles = new();

        private static readonly Color[] Colors =
        {
            Color.FromRgb(233,  69,  96),
            Color.FromRgb( 39, 174,  96),
            Color.FromRgb(243, 156,  18),
            Color.FromRgb( 52, 152, 219),
            Color.FromRgb(155,  89, 182),
            Color.FromRgb(241, 196,  15),
        };

        public VictoryOverlay(string winnerText)
        {
            InitializeComponent();
            TxtWinner.Text = winnerText;
            SpawnConfetti();
            StartAnimation();
        }

        private void SpawnConfetti()
        {
            for (int i = 0; i < 60; i++)
            {
                var el = new Ellipse
                {
                    Width = _rnd.Next(6, 14),
                    Height = _rnd.Next(6, 14),
                    Fill = new SolidColorBrush(
                                 Colors[_rnd.Next(Colors.Length)])
                };
                double x = _rnd.NextDouble() * 600;
                double y = -20 - _rnd.NextDouble() * 200;
                Canvas.SetLeft(el, x);
                Canvas.SetTop(el, y);
                ConfettiCanvas.Children.Add(el);

                double vx = (_rnd.NextDouble() - 0.5) * 3;
                double vy = 2 + _rnd.NextDouble() * 3;
                _particles.Add((el, vx, vy));
            }
        }

        private void StartAnimation()
        {
            _confettiTimer.Interval = TimeSpan.FromMilliseconds(16);
            _confettiTimer.Tick += (s, e) =>
            {
                for (int i = 0; i < _particles.Count; i++)
                {
                    var (el, vx, vy) = _particles[i];
                    double x = Canvas.GetLeft(el) + vx;
                    double y = Canvas.GetTop(el) + vy;
                    Canvas.SetLeft(el, x);
                    Canvas.SetTop(el, y);
                    if (y > 420)
                    {
                        Canvas.SetTop(el, -14);
                        Canvas.SetLeft(el, _rnd.NextDouble() * 600);
                    }
                }
            };
            _confettiTimer.Start();

            // Автозакрытие через 8 сек
            var autoClose = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
            autoClose.Tick += (s, e) => { autoClose.Stop(); Close(); };
            autoClose.Start();
        }

        private void BtnPlayAgain_Click(object sender, RoutedEventArgs e)
        {
            PlayAgain = true;
            _confettiTimer.Stop();
            Close();
        }

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            PlayAgain = false;
            _confettiTimer.Stop();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _confettiTimer.Stop();
            base.OnClosed(e);
        }
    }
}
