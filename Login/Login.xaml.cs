using System.Windows;
using Microsoft.Data.SqlClient;
using System.Data;

namespace KeepIT
{
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void btn_LOGIN_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Unesi korisničko ime i lozinku.");
                return;
            }

            string connectionString =
                "Server=tcp:keep-it.database.windows.net,1433;" +
                "Initial Catalog=KeepIT;" +
                "Persist Security Info=False;" +
                "User ID=fbarisicAzure;" +
                "Password=FBarisic123!;" +
                "MultipleActiveResultSets=False;" +
                "Encrypt=True;" +
                "TrustServerCertificate=False;" +
                "Connection Timeout=30;";

            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();

                const string query = @"
                    SELECT TOP (1) UserId, Username
                    FROM dbo.Korisnici
                    WHERE Username COLLATE Latin1_General_100_CS_AS = @username
                      AND PasswordHash = HASHBYTES('SHA2_256', @password);
                    ";

                using var cmd = new SqlCommand(query, conn);

                cmd.Parameters.Add("@username", SqlDbType.NVarChar, 64).Value = username;
                cmd.Parameters.Add("@password", SqlDbType.NVarChar, 200).Value = password;

                using var r = cmd.ExecuteReader();
                if (!r.Read())
                {
                    MessageBox.Show("Neispravno korisničko ime ili lozinka.");
                    return;
                }

                var userId = (Guid)r["UserId"];
                var dbUsername = (string)r["Username"];
                ((App)Application.Current).SetCurrentUser(dbUsername);
                ((App)Application.Current).SetCurrentUserId(userId);

                MainMenu mainMenu = new MainMenu();
                mainMenu.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri pristupu bazi: " + ex.Message);
            }
        }

        private void btn_Close_Click(object sender, RoutedEventArgs e) => Close();
        private void btn_Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    }
}
