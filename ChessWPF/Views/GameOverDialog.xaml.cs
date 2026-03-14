using System.Windows;

namespace ChessWPF.Views
{
    public partial class GameOverDialog : Window
    {
        public GameOverDialog(string message)
        {
            InitializeComponent();
            TxtResult.Text = message.Contains("Победа") || message.Contains("МАТ") ? "♟ МАТ!" :
                              message.Contains("Пат") || message.Contains("Ничья") ? "Ничья" : "Игра окончена";
            TxtMessage.Text = message;
        }

        private void BtnNewGame_Click(object sender, RoutedEventArgs e) { DialogResult = true; }
        private void BtnMenu_Click(object sender, RoutedEventArgs e) { DialogResult = false; }
    }
}
