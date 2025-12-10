using System.Windows;

namespace KeepIT
{
    public partial class LogoutDialog : Window
    {
        public LogoutChoice Choice { get; private set; } = LogoutChoice.None;

        public LogoutDialog()
        {
            InitializeComponent();
        }

        private void btn_Logout_Click(object sender, RoutedEventArgs e)
        {
            Choice = LogoutChoice.Logout;
            DialogResult = true;
        }

        private void btn_LogoutAndExit_Click(object sender, RoutedEventArgs e)
        {
            Choice = LogoutChoice.LogoutAndExit;
            DialogResult = true;
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Choice = LogoutChoice.Cancel;
            DialogResult = false;
        }
    }
}
