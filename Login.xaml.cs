using System.Windows;
using Microsoft.Data.SqlClient;

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
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Neispravno korisničko ime ili lozinka.");
                return;
            }

            string connectionString =
                @"Data Source=BAKS\SQLEXPRESS;Initial Catalog=KeepIT;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Application Name=""SQL Server Management Studio"";";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    string query = "SELECT COUNT(1) FROM Korisnici WHERE Username=@username AND Password=@password";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        if (count == 1)
                        {
                            MainMenu mainMenu = new MainMenu();
                            mainMenu.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Neispravno korisničko ime ili lozinka.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Greška pri pristupu bazi: " + ex.Message);
                }
            }
        }
    }
}