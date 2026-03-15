using System.Windows;
using System.Windows.Controls;
using ChessWPF.Models;

namespace ChessWPF.Views
{
    public partial class MainMenuView : Page
    {
        public MainMenuView()
        {
            InitializeComponent();
            TxtWelcome.Text =
                $"Добро пожаловать, {PlayerProfile.Instance.Nickname}!";
        }

        private void BtnVsAI_Click(object sender, RoutedEventArgs e)
        {
            var diffDlg = new DifficultyDialog();
            diffDlg.Owner = MainWindow.Instance;
            if (diffDlg.ShowDialog() != true) return;

            var timeDlg = new TimeDialog();
            timeDlg.Owner = MainWindow.Instance;
            if (timeDlg.ShowDialog() != true) return;

            MainWindow.Instance.NavigateToGame(
                GameMode.VsAI,
                aiDepth: diffDlg.SelectedDepth,
                timerSeconds: timeDlg.SelectedSeconds);
        }

        private void BtnLan_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new LanSetupDialog();
            dlg.Owner = MainWindow.Instance;
            if (dlg.ShowDialog() == true)
            {
                if (dlg.IsHost)
                    MainWindow.Instance.NavigateToGame(GameMode.LanHost);
                else
                    MainWindow.Instance.NavigateToGame(
                        GameMode.LanClient,
                        networkAddress: dlg.ServerIP);
            }
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
            => MainWindow.Instance.NavigateToProfile();

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
            => MainWindow.Instance.NavigateToHistory();

        private void BtnPuzzles_Click(object sender, RoutedEventArgs e)
            => MainWindow.Instance.NavigateToPuzzles();

        private void BtnExit_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();
    }
}
