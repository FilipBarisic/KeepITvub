using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Input;

namespace KeepIT
{
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }


        // Trajanje SAS tokena za Azure Blob container, nakon čega će korisnik morati ponovo loginirati da bi dobio novi token.
        private static readonly TimeSpan ContainerSasDuration = TimeSpan.FromHours(8);
        private sealed class AuthenticatedUser
        {
            public Guid UserId { get; }
            public string Username { get; }
            public string ContainerUser { get; }

            public AuthenticatedUser(Guid userId, string username, string containerUser)
            {
                UserId = userId;
                Username = username;
                ContainerUser = containerUser;
            }
        }

        // Metoda za autentikaciju korisnika prema DB.
        private static async Task<AuthenticatedUser?> AuthenticateAsync(string username, string password)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var sqlConnectionString = await KeyVaultSecrets.GetSqlConnectionStringAppAsync();
            await using var connection = new SqlConnection(sqlConnectionString);
            await connection.OpenAsync(cts.Token);

            const string query = @"
                    SELECT TOP (1) UserId, Username, ContainerUser
                    FROM dbo.Korisnici
                    WHERE Username COLLATE Latin1_General_100_CS_AS = @username
                    AND PasswordHash = HASHBYTES('SHA2_256', @password);";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@username", SqlDbType.NVarChar, 64).Value = username;
            command.Parameters.Add("@password", SqlDbType.NVarChar, 200).Value = password;

            await using var reader = await command.ExecuteReaderAsync(cts.Token);
            if (!await reader.ReadAsync(cts.Token))
                return null;

            var userId = (Guid)reader["UserId"];
            var dbUsername = (string)reader["Username"];
            var containerUser = ((string)reader["ContainerUser"]).Trim().ToLowerInvariant();

            return new AuthenticatedUser(userId, dbUsername, containerUser);
        }

        // UI button za login, osigurava Azure Blob container.
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Alerts.Show(this, "Unesi korisničko ime i lozinku.", "LOGIN");
                return;
            }

            try
            {
                var user = await AuthenticateAsync(username, password);
                if (user is null)
                {
                    Alerts.Show(this, "Neispravno korisničko ime ili lozinka.", "LOGIN");
                    return;
                }

                var app = (App)Application.Current;
                app.SetCurrentUser(user.Username);
                app.SetCurrentUserId(user.UserId);
                app.SetCurrentContainerUser(user.ContainerUser);

                var accountName = await KeyVaultSecrets.GetStorageAccountNameAsync();
                var accountKey = await KeyVaultSecrets.GetStorageAccountKeyAsync();

                await AzureBlobHelper.EnsureContainerExistsAsync(accountName, accountKey, user.ContainerUser);

                var sasUri = AzureBlobHelper.BuildContainerSasUri(
                    accountName,
                    accountKey,
                    user.ContainerUser,
                    ContainerSasDuration);

                app.SetCurrentContainerSasUri(sasUri);

                AutoArchiveService.OnUserChanged();

                new MainMenu().Show();
                Close();
            }
            catch (SqlException ex)
            {
                Alerts.Show(this, "SQL greška: " + ex.Message, "GREŠKA");
            }
            catch (Exception ex)
            {
                Alerts.Show(this, "Opća greška: " + ex.Message, "GREŠKA");
            }
        }

        //UI buttons za zatvaranje, minimiziranje i pomicanje prozora.
        private void CloseButton_Click(object sender, RoutedEventArgs e) { Close(); }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }
        private void TopBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) { DragMove(); } }
    }
}