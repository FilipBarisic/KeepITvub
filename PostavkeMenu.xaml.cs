using System.Windows;

namespace KeepIT
{

    public partial class PostavkeMenu : Window
    {
        public PostavkeMenu()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainMenu mainMenu = new MainMenu();
            mainMenu.Show();
            this.Close();
        }
    }
}
