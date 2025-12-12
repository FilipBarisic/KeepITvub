using System.Windows;
using System.Windows.Controls;

namespace KeepIT
{
    public partial class LOCALsaveMenu : Window
    {
        public LOCALsaveMenu()
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

        private void tvFolders_SelectedItemChanged()
        {
            WindowState = WindowState.Normal;
        }
    }
}
