using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace KeepIT
{
    public static class KeyVaultSecrets
    {
        private static readonly Uri VaultUri = new Uri("https://keepitkeyvault.vault.azure.net/");

        private const string TenantId = "6c1ac70f-a58b-46e7-81fa-2724f6187b98";
        private const string ClientId = "82da9bbd-ea03-4ae2-8bd7-0afb8cabef3d";

        private static readonly string AppDataDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KeepIT");

        private static readonly string AuthRecordPath =
            Path.Combine(AppDataDir, "authrecord.bin");

        private static readonly TokenCachePersistenceOptions CacheOptions =
            new TokenCachePersistenceOptions { Name = "KeepIT" };

        private static readonly ConcurrentDictionary<string, string> Cache = new();
        private static readonly Lazy<Task<SecretClient>> Client = new(CreateClientAsync);

        private static async Task<SecretClient> CreateClientAsync()
        {
            Directory.CreateDirectory(AppDataDir);

            var options = new InteractiveBrowserCredentialOptions
            {
                TenantId = TenantId,
                ClientId = ClientId,
                TokenCachePersistenceOptions = CacheOptions
            };

            if (File.Exists(AuthRecordPath))
            {
                await using var s = File.OpenRead(AuthRecordPath);
                options.AuthenticationRecord = await AuthenticationRecord.DeserializeAsync(s);
                return new SecretClient(VaultUri, new InteractiveBrowserCredential(options));
            }

            var credential = new InteractiveBrowserCredential(options);
            var record = await credential.AuthenticateAsync();

            await using (var outS = File.Create(AuthRecordPath))
                await record.SerializeAsync(outS);

            options.AuthenticationRecord = record;
            return new SecretClient(VaultUri, new InteractiveBrowserCredential(options));
        }

        private static async Task<string> GetAsync(string name)
        {
            if (Cache.TryGetValue(name, out var cached))
                return cached;

            var client = await Client.Value;
            var response = await client.GetSecretAsync(name);
            var value = response.Value.Value;

            Cache[name] = value;
            return value;
        }

        public static Task<string> GetSqlConnectionStringStartupAsync() =>
            GetAsync("keepit-sql-connection-startup");

        public static Task<string> GetSqlConnectionStringAppAsync() =>
            GetAsync("keepit-sql-connection-app");

        public static Task<string> GetStorageAccountNameAsync() =>
            GetAsync("keepit-storage-account-name");

        public static Task<string> GetStorageAccountKeyAsync() =>
            GetAsync("keepit-storage-account-key");
    }
}