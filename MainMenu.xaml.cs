using System.Windows;

namespace KeepIT
{

    public partial class MainMenu : Window
    {
        public MainMenu()
        {
            InitializeComponent();
            var app = (App)Application.Current;
            txtWelcome.Text = $"Dobrodošli - {app.CurrentUsername}";

        }

        private void btn_Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btn_LOCAL_Click(object sender, RoutedEventArgs e)
        {
            LOCALsaveMenu localmenu = new LOCALsaveMenu();
            localmenu.Show();
            this.Close();
        }

        private void btn_SERVER_Click(object sender, RoutedEventArgs e)
        {
            SERVERsaveMenu serversavemenu = new SERVERsaveMenu();
            serversavemenu.Show();
            this.Close();
        }

        private void btn_POSTAVKE_Click(object sender, RoutedEventArgs e)
        {
            PostavkeMenu postavkemenu = new PostavkeMenu();
            postavkemenu.Show();
            this.Close();
        }
    }
}
