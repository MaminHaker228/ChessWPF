using System.Windows;

namespace ChessWPF.Views
{
    public partial class DifficultyDialog : Window
    {
        public int SelectedDepth { get; private set; } = 3;

        public DifficultyDialog()
        {
            InitializeComponent();
        }

        private void BtnEasy_Click(object sender, RoutedEventArgs e)
        {
            SelectedDepth = 1;
            DialogResult = true;
        }

        private void BtnMedium_Click(object sender, RoutedEventArgs e)
        {
            SelectedDepth = 3;
            DialogResult = true;
        }

        private void BtnHard_Click(object sender, RoutedEventArgs e)
        {
            SelectedDepth = 5;
            DialogResult = true;
        }
    }
}
