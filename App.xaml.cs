using System.Windows;

namespace KeepIT
{
    public partial class App : Application
    {
        public string? CurrentUsername { get; private set; }
        public Guid CurrentUserId { get; private set; } = Guid.Empty;

        public bool IsLoggedIn
            => !string.IsNullOrWhiteSpace(CurrentUsername);

        public void SetCurrentUser(string username)
        {
            CurrentUsername = username;
        }

        public void SetCurrentUserId(Guid userId)
        {
            CurrentUserId = userId;
        }

        public void ClearSession()
        {
            CurrentUsername = null;
            CurrentUserId = Guid.Empty;
        }
    }
}
