using System.Windows;

namespace ChessWPF.Views
{
    public partial class TimeDialog : Window
    {
        public int SelectedSeconds { get; private set; } = 0; // 0 = без таймера

        public TimeDialog()
        {
            InitializeComponent();
        }

        private void Btn1_Click(object sender, RoutedEventArgs e)
        {
            SelectedSeconds = 60;
            DialogResult = true;
        }

        private void Btn3_Click(object sender, RoutedEventArgs e)
        {
            SelectedSeconds = 180;
            DialogResult = true;
        }

        private void Btn5_Click(object sender, RoutedEventArgs e)
        {
            SelectedSeconds = 300;
            DialogResult = true;
        }

        private void Btn10_Click(object sender, RoutedEventArgs e)
        {
            SelectedSeconds = 600;
            DialogResult = true;
        }

        private void BtnNoTimer_Click(object sender, RoutedEventArgs e)
        {
            SelectedSeconds = 0;
            DialogResult = true;
        }
    }
}
