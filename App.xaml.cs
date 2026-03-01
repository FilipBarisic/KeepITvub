using System;
using System.Windows;

namespace KeepIT
{
    public partial class App : Application
    {
        public string? CurrentUsername { get; private set; }
        public Guid CurrentUserId { get; private set; } = Guid.Empty;
        public string? CurrentContainerUser { get; private set; }
        public Uri? CurrentContainerSasUri { get; private set; }

        public bool IsLoggedIn => !string.IsNullOrWhiteSpace(CurrentUsername);

        public void SetCurrentUser(string username) => SetCurrentUsername(username);
        public void SetCurrentUserId(Guid userId) => CurrentUserId = userId;
        public void SetCurrentContainerUser(string containerUser) => CurrentContainerUser = containerUser;
        public void SetCurrentContainerSasUri(Uri sasUri) => CurrentContainerSasUri = sasUri;

        public void SetCurrentUsername(string username)
        {
            CurrentUsername = string.IsNullOrWhiteSpace(username) ? null : username.Trim();
        }

        public void SetSession(string username, Guid userId, string containerUser, Uri containerSasUri)
        {
            SetCurrentUsername(username);
            CurrentUserId = userId;
            CurrentContainerUser = string.IsNullOrWhiteSpace(containerUser) ? null : containerUser.Trim();
            CurrentContainerSasUri = containerSasUri;
        }

        public void ClearSession()
        {
            CurrentUsername = null;
            CurrentUserId = Guid.Empty;
            CurrentContainerUser = null;
            CurrentContainerSasUri = null;

            AutoArchiveService.OnUserChanged();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AutoArchiveService.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AutoArchiveService.Stop();
            base.OnExit(e);
        }
    }
}