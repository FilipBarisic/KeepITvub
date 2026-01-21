using System.Windows;

namespace KeepIT
{
    public partial class ChangeLocalSave : Window
    {
        public ChangeLocalSave()
        {
            InitializeComponent();

            txtCurrentLocalSavePath.Text = DefaultArchivePathStore.Get();
        }

        private void btn_PromjeniLocalSave_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Odaberi novu default lokaciju arhiviranja",
                Multiselect = false
            };

            if (ofd.ShowDialog(this) != true) return;

            DefaultArchivePathStore.Set(ofd.FolderName);
            txtCurrentLocalSavePath.Text = DefaultArchivePathStore.Get();
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
