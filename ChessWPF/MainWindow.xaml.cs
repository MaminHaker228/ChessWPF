using System.Windows;
using ChessWPF.Views;

namespace ChessWPF
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            NavigateToMainMenu();
        }

        public void NavigateToMainMenu()
        {
            MainFrame.Navigate(new MainMenuView());
        }

        public void NavigateToGame(GameMode mode, string networkAddress = null)
        {
            MainFrame.Navigate(new GameView(mode, networkAddress));
        }
    }

    public enum GameMode
    {
        VsAI,
        LanHost,
        LanClient
    }
}
