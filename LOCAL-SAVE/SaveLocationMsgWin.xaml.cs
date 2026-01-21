using System.Windows;

namespace KeepIT
{
    public partial class SaveLocationMsgWin : Window
    {
        public SaveLocationMsgWin(string message, string title = "Spremljeno")
        {
            InitializeComponent();
            txtTitle.Text = title;
            txtMessage.Text = message;
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
