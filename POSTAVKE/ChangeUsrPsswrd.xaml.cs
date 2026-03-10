using System.Data;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.SqlClient;

namespace KeepIT
{
    public partial class ChangeUsrPsswrd : Window
    {

        public ChangeUsrPsswrd() { InitializeComponent(); }

        private async void PromijeniLozinku_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;

            var username = app.CurrentUsername;
            var userId = app.CurrentUserId;

            var oldPassword = OldPasswordBox.Password;
            var newPassword = NewPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username))
            {
                Alerts.Show(this, "Nisi prijavljen.", "POSTAVKE");
                return;
            }

            if (userId == Guid.Empty)
            {
                Alerts.Show(this, "Sesija nije ispravna. Prijavi se ponovno.", "POSTAVKE");
                return;
            }

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                Alerts.Show(this, "Popuni staru i novu lozinku.", "POSTAVKE");
                return;
            }

            var button = (sender as FrameworkElement);
            if (button != null)
                button.IsEnabled = false;

            try
            {
                var changed = await PromijeniLozinkuAsync(userId, oldPassword, newPassword);

                if (changed)
                {
                    Alerts.Show(this, "Lozinka je promijenjena. Potrebno se ponovno prijaviti!", "USPJEH");

                    OldPasswordBox.Clear();
                    NewPasswordBox.Clear();

                    new Login().Show();
                    Close();
                }
                else
                {
                    Alerts.Show(this, "Stara lozinka nije točna.", "GREŠKA");
                }
            }
            catch (SqlException ex)
            {
                Alerts.Show(this, "Greška pri pristupu bazi: " + ex.Message, "GREŠKA");
            }
            catch (Exception ex)
            {
                Alerts.Show(this, "Greška: " + ex.Message, "GREŠKA");
            }
            finally
            {
                if (button != null)
                    button.IsEnabled = true;
            }
        }
        private static async Task<bool> PromijeniLozinkuAsync(Guid userId, string oldPassword, string newPassword)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var sqlConnectionString = await KeyVaultSecrets.GetSqlConnectionStringAppAsync();
            await using var connection = new SqlConnection(sqlConnectionString);
            await connection.OpenAsync(cts.Token);

            const string sql = @"
                        UPDATE dbo.Korisnici
                        SET PasswordHash = HASHBYTES('SHA2_256', @newPassword)
                        WHERE UserId = @userId
                        AND PasswordHash = HASHBYTES('SHA2_256', @oldPassword);";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@userId", SqlDbType.UniqueIdentifier).Value = userId;
            command.Parameters.Add("@oldPassword", SqlDbType.NVarChar, 200).Value = oldPassword;
            command.Parameters.Add("@newPassword", SqlDbType.NVarChar, 200).Value = newPassword;

            var rows = await command.ExecuteNonQueryAsync(cts.Token);
            return rows == 1;
        }

        private void TopBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            new PostavkeMenu().Show();
            Close();
        }
    }
}