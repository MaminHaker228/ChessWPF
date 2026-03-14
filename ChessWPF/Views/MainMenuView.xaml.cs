using System.Windows;
using System.Windows.Controls;

namespace ChessWPF.Views
{
    public partial class MainMenuView : Page
    {
        public MainMenuView()
        {
            InitializeComponent();
        }

        private void BtnVsAI_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.NavigateToGame(GameMode.VsAI);
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
                    MainWindow.Instance.NavigateToGame(GameMode.LanClient, dlg.ServerIP);
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
