using System.Windows;

namespace ChessWPF.Views
{
    public partial class LanSetupDialog : Window
    {
        public bool IsHost { get; private set; }
        public string ServerIP { get; private set; }

        public LanSetupDialog()
        {
            InitializeComponent();
        }

        private void BtnHost_Click(object sender, RoutedEventArgs e)
        {
            IsHost = true;
            DialogResult = true;
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            IsHost = false;
            ServerIP = TxtIP.Text.Trim();
            DialogResult = true;
        }
    }
}
