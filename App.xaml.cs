using System.Windows;

namespace KeepIT
{
    public partial class App : Application
    {
        public string CurrentUsername { get; private set; }

        public bool IsLoggedIn
            => !string.IsNullOrWhiteSpace(CurrentUsername);

        public void SetCurrentUser(string username)
        {
            CurrentUsername = username;
        }

        public void ClearSession()
        {
            CurrentUsername = null;
        }
    }
}
