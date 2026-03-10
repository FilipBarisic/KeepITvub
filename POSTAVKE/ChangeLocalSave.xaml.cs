using System.Windows;
using System.Windows.Input;

namespace KeepIT
{
    public partial class ChangeLocalSave : Window
    {
        public ChangeLocalSave()
        {
            InitializeComponent();
            UcitajPath();
        }

        private void UcitajPath() { CurrentLocalSavePathText.Text = DefaultArchivePathStore.Get(); }
        private void PromijeniDefaultPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Odaberi novu default lokaciju arhiviranja",
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != true)
                return;

            DefaultArchivePathStore.Set(dialog.FolderName);
            UcitajPath();
        }

        private void TopBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            new PostavkeMenu().Show();
            Close();
        }
    }
}