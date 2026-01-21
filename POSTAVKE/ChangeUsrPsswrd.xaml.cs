using System.Windows;

namespace KeepIT
{
    public partial class ChangeUsrPsswrd : Window
    {
        public ChangeUsrPsswrd()
        {
            InitializeComponent();
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

        private void btn_PromjeniPostavkeProfila_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }
    }
}
