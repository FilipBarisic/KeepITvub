using System.Windows;

namespace KeepIT
{
    public partial class LocalLocationPick : Window
    {
        public SaveChoice Choice { get; private set; } = SaveChoice.None;

        public LocalLocationPick()
        {
            InitializeComponent();
        }

        private void btn_CostumSave_Click(object sender, RoutedEventArgs e)
        {
            Choice = SaveChoice.CostumSave;
            DialogResult = true;
        }

        private void btn_DefaultSave_Click(object sender, RoutedEventArgs e)
        {
            Choice = SaveChoice.DefaultSave;
            DialogResult = true;
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Choice = SaveChoice.Cancel;
            DialogResult = false;
        }

    }
}
