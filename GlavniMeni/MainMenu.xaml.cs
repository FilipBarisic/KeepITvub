using System.Windows;
using System.Windows.Input;

namespace KeepIT
{
    public partial class MainMenu : Window
    {
        public MainMenu()
        {
            InitializeComponent();
            SetWelcomeMessage();
        }

        private void SetWelcomeMessage()
        {
            var app = (App)Application.Current;

            WelcomeText.Text = app.IsLoggedIn
                ? $"Dobrodošli - {app.CurrentUsername}"
                : "Korisnik nije logiran, ulogiraj korisnika";
        }

        // Gumbi za zatvaranje, minimiziranje i pomicanje prozora
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var choice = Alerts.Choose(
                this,
                "Odaberi radnju:",
                "ODJAVA",
                primaryText: "ODJAVA",
                secondaryText: "ODJAVA + IZLAZ",
                cancelText: "ODUSTANI");

            if (choice == AlertResult.Tertiary || choice == AlertResult.Closed)
                return;

            var app = (App)Application.Current;
            app.ClearSession();

            if (choice == AlertResult.Primary)
            {
                new Login().Show();
                Close();
                return;
            }

            if (choice == AlertResult.Secondary)
            {
                Application.Current.Shutdown();
            }
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }
        private void TopBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) { DragMove(); } }

        // Navigacija na ostale prozore
        private void ArchiveLocalButton_Click(object sender, RoutedEventArgs e)
        {
            new LOCALsaveMenu().Show();
            Close();
        }
        private void ArchiveServerButton_Click(object sender, RoutedEventArgs e)
        {
            new SERVERsaveMenu().Show();
            Close();
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new PostavkeMenu().Show();
            Close();
        }
    }
}