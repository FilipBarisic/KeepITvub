using System.Windows;
using System.Windows.Input;

namespace KeepIT
{
    public partial class PostavkeMenu : Window
    {
        public PostavkeMenu()
        {
            InitializeComponent();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            new MainMenu().Show();
            Close();
        }

        private void TopBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void ChangeLocalSaveButton_Click(object sender, RoutedEventArgs e)
        {
            new ChangeLocalSave().Show();
            Close();
        }

        private void PromjeniLozinku_Click(object sender, RoutedEventArgs e)
        {
            new ChangeUsrPsswrd().Show();
            Close();
        }

        private void AutoArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            new AutoArhiviranje().Show();
            Close();
        }
    }
}