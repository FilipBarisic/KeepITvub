using System.Windows;

namespace KeepIT
{
    public partial class ArchiveCount : Window
    {
        public ArchiveCount(string message, string title = "Upozorenje")
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
