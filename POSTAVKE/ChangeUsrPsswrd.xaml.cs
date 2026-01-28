using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;

namespace KeepIT
{
    public partial class ChangeUsrPsswrd : Window
    {
        private const string ConnectionString =
            "Server=tcp:keep-it.database.windows.net,1433;" +
            "Initial Catalog=KeepIT;" +
            "Persist Security Info=False;" +
            "User ID=fbarisicAzure;" +
            "Password=FBarisic123!;" +
            "MultipleActiveResultSets=False;" +
            "Encrypt=True;" +
            "TrustServerCertificate=False;" +
            "Connection Timeout=30;";

        public ChangeUsrPsswrd()
        {
            InitializeComponent();
        }

        private void btn_PromjeniPostavkeProfila_Click(object sender, RoutedEventArgs e)
        {
            string username = ((App)Application.Current).CurrentUsername!;
            Guid userId = ((App)Application.Current).CurrentUserId;

            string oldPass = txt_OldPsswrd.Password;
            string newPass = txt_NewPsswrd.Password;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Nisi prijavljen.");
                return;
            }

            if (string.IsNullOrWhiteSpace(oldPass) || string.IsNullOrWhiteSpace(newPass))
            {
                MessageBox.Show("Popuni staru i novu lozinku.");
                return;
            }

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();

                const string sql = @"
                    UPDATE dbo.Korisnici
                    SET PasswordHash = HASHBYTES('SHA2_256', @newPassword)
                    WHERE Username COLLATE Latin1_General_100_CS_AS = @username
                      AND PasswordHash = HASHBYTES('SHA2_256', @oldPassword);
                    ";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@username", SqlDbType.NVarChar, 64).Value = username;
                cmd.Parameters.Add("@oldPassword", SqlDbType.NVarChar, 200).Value = oldPass;
                cmd.Parameters.Add("@newPassword", SqlDbType.NVarChar, 200).Value = newPass;

                int rows = cmd.ExecuteNonQuery();

                if (rows == 1)
                {
                    MessageBox.Show("Lozinka je promijenjena. Potrebno se ponovno prijaviti!");
                    txt_OldPsswrd.Clear();
                    txt_NewPsswrd.Clear();

                    var login = new Login();
                    login.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Stara lozinka nije točna.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri pristupu bazi: " + ex.Message);
            }
        }

        private void btn_Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void btn_Back_Click(object sender, RoutedEventArgs e)
        {
            PostavkeMenu postavkeMenu = new PostavkeMenu();
            postavkeMenu.Show();
            this.Close();
        }
    }
}
