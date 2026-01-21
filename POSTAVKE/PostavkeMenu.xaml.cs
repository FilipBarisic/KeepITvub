using System.Windows;

namespace KeepIT
{

    public partial class PostavkeMenu : Window
    {
        public PostavkeMenu()
        {
            InitializeComponent();
        }

        private void btn_Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized; // Minimizira prozor  ali probaj napravi taskbar ikonu
        }
        private void btn_Back_Click(object sender, RoutedEventArgs e)
        {
            MainMenu mainMenu = new MainMenu();
            mainMenu.Show();
            this.Close();
        }

        private void btn_ChangeLocalSave_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }

        private void btn_ChangeUsernamePassword_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }
    }
}
