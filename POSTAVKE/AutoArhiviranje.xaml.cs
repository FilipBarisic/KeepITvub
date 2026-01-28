using System.Windows;

namespace KeepIT
{
    public partial class AutoArhiviranje : Window
    {
        public AutoArhiviranje()
        {
            InitializeComponent();
        }

        private void btn_PostaviArhiviranje_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }

        private void btn_UkloniArhiviranje_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }

        private void btn_Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized; // Minimizira prozor  ali probaj napravi taskbar ikonu
        }
        private void btn_Back_Click(object sender, RoutedEventArgs e)
        {
            PostavkeMenu postavkeMenu = new PostavkeMenu();
            postavkeMenu.Show();
            this.Close();
        }
    }
}
