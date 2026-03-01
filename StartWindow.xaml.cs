using System.Windows;
using Microsoft.Data.SqlClient;

namespace KeepIT
{
    public partial class StartWindow : Window
    {
        public StartWindow()
        {
            InitializeComponent();
            Loaded += StartWindow_Loaded;
        }

        private async void StartWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;

            var connectionString = await KeyVaultSecrets.GetSqlConnectionStringStartupAsync();
            var canConnect = await CanConnectToDatabaseAsync(connectionString);
            if (!canConnect)
            {
                // Ako se ne može povezati na bazu, obavijestiti korisnika i zatvoriti aplikaciju.
                Alerts.Show(this, "Ne mogu se povezati na bazu podataka. Provjerite internetsku vezu i pokušajte ponovo.", "Upozorenje");
                Application.Current.Shutdown();
                return;
            }

            var loginWindow = new Login();
            loginWindow.Show();
            Close();
        }

        // Jednostavni test konekcije prema DB, da se ne bi LOGIN srušio ili trajao dugo ako DB nije dostupan.
        private static async Task<bool> CanConnectToDatabaseAsync(string connectionString)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cts.Token);

                using var command = new SqlCommand("SELECT 1", connection);
                var scalar = await command.ExecuteScalarAsync(cts.Token);

                return scalar != null
                       && int.TryParse(scalar.ToString(), out var value)
                       && value == 1;
            }
            catch
            {
                return false;
            }
        }
    }
}