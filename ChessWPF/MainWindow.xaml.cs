using System.Windows;
using ChessWPF.Models;
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
            AppTheme.LoadSaved(); // ← загрузка темы
            NavigateToMainMenu();
        }

        public void NavigateToMainMenu() => MainFrame.Navigate(new MainMenuView());
        public void NavigateToProfile() => MainFrame.Navigate(new ProfileView());
        public void NavigateToHistory() => MainFrame.Navigate(new HistoryView());
        public void NavigateToPuzzles() => MainFrame.Navigate(new PuzzleView());
        public void NavigateToAnalysis(GameRecord r) => MainFrame.Navigate(new AnalysisView(r));

        public void NavigateToGame(GameMode mode,
                                   string networkAddress = null,
                                   int aiDepth = 3,
                                   int timerSeconds = 0)
            => MainFrame.Navigate(
                new GameView(mode, networkAddress, aiDepth, timerSeconds));
    }

    public enum GameMode { VsAI, LanHost, LanClient }
}
