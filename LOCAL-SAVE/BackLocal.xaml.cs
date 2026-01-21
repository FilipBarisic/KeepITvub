using System.Windows;

namespace KeepIT
{
    public partial class BackLocal : Window
    {
        public BackLocal(string message, string title = "Upozorenje")
        {
            InitializeComponent();
            txtTitle.Text = title;
            txtMessage.Text = message;
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
