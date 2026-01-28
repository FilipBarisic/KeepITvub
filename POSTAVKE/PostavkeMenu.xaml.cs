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
            ChangeLocalSave chnglclsve = new ChangeLocalSave();
            chnglclsve.Show();
            this.Close();
        }

        private void btn_ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ChangeUsrPsswrd pswrchng = new ChangeUsrPsswrd();
            pswrchng.Show();
            this.Close();
        }

        private void btn_PostaviAutoArhiviranje_Click(object sender, RoutedEventArgs e)
        {
            AutoArhiviranje autoArhiva = new AutoArhiviranje();
            autoArhiva.Show();
            this.Close();
        }
    }
}
