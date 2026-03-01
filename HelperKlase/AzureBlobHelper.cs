using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using System.IO;

namespace KeepIT
{
    public static class AzureBlobHelper
    {
        public static async Task EnsureContainerExistsAsync(
            string accountName,
            string accountKey,
            string containerName,
            CancellationToken cancellationToken = default)
        {
            var normalizedContainer = NormalizeContainerName(containerName);

            var service = CreateServiceClient(accountName, accountKey);
            var container = service.GetBlobContainerClient(normalizedContainer);

            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        }

        public static Uri BuildContainerSasUri(
            string accountName,
            string accountKey,
            string containerName,
            TimeSpan validFor)
        {
            var normalizedAccount = NormalizeAccountName(accountName);
            var normalizedContainer = NormalizeContainerName(containerName);

            if (validFor <= TimeSpan.Zero)
                throw new ArgumentException("SAS trajanje mora biti veće od nule.", nameof(validFor));

            var credential = CreateSharedKeyCredential(normalizedAccount, accountKey);

            var sas = new BlobSasBuilder
            {
                BlobContainerName = normalizedContainer,
                Resource = "c",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.Add(validFor),
            };

            sas.SetPermissions(
                BlobSasPermissions.Read |
                BlobSasPermissions.Write |
                BlobSasPermissions.Create |
                BlobSasPermissions.List);

            var token = sas.ToSasQueryParameters(credential).ToString();
            return new Uri($"https://{normalizedAccount}.blob.core.windows.net/{normalizedContainer}?{token}");
        }

        public static async Task UploadZipToUserContainerAsync(
            Uri containerSasUri,
            string localZipPath,
            string blobName,
            CancellationToken cancellationToken = default)
        {
            if (containerSasUri == null)
                throw new ArgumentNullException(nameof(containerSasUri));

            if (string.IsNullOrWhiteSpace(localZipPath))
                throw new ArgumentException("Zip putanja je prazna.", nameof(localZipPath));

            if (string.IsNullOrWhiteSpace(blobName))
                throw new ArgumentException("Blob name je prazan.", nameof(blobName));

            var containerClient = new BlobContainerClient(containerSasUri);
            var blobClient = containerClient.GetBlobClient(NormalizeBlobName(blobName));

            await using var fs = File.OpenRead(localZipPath);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/zip" },
                AccessTier = AccessTier.Hot
            };

            await blobClient.UploadAsync(fs, options, cancellationToken);
        }

        public static async Task<List<BlobItem>> ListBlobsAsync(
            Uri containerSasUri,
            string? prefix = null,
            CancellationToken cancellationToken = default)
        {
            if (containerSasUri == null)
                throw new ArgumentNullException(nameof(containerSasUri));

            var containerClient = new BlobContainerClient(containerSasUri);
            var results = new List<BlobItem>();

            await foreach (var blob in containerClient.GetBlobsAsync(
                               traits: BlobTraits.None,
                               states: BlobStates.None,
                               prefix: string.IsNullOrWhiteSpace(prefix) ? null : prefix,
                               cancellationToken: cancellationToken))
            {
                results.Add(blob);
            }

            return results;
        }

        public static async Task DownloadBlobToFileAsync(
            Uri containerSasUri,
            string blobName,
            string localFilePath,
            CancellationToken cancellationToken = default)
        {
            if (containerSasUri == null)
                throw new ArgumentNullException(nameof(containerSasUri));

            if (string.IsNullOrWhiteSpace(blobName))
                throw new ArgumentException("Blob name je prazan.", nameof(blobName));

            if (string.IsNullOrWhiteSpace(localFilePath))
                throw new ArgumentException("Lokalna putanja je prazna.", nameof(localFilePath));

            var containerClient = new BlobContainerClient(containerSasUri);
            var blobClient = containerClient.GetBlobClient(NormalizeBlobName(blobName));

            var dir = Path.GetDirectoryName(localFilePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            await blobClient.DownloadToAsync(localFilePath, cancellationToken);
        }

        private static BlobServiceClient CreateServiceClient(string accountName, string accountKey)
        {
            var normalizedAccount = NormalizeAccountName(accountName);
            var credential = CreateSharedKeyCredential(normalizedAccount, accountKey);

            var serviceUri = new Uri($"https://{normalizedAccount}.blob.core.windows.net");
            return new BlobServiceClient(serviceUri, credential);
        }

        private static StorageSharedKeyCredential CreateSharedKeyCredential(string accountName, string accountKey)
        {
            var normalizedAccount = NormalizeAccountName(accountName);
            var normalizedKey = (accountKey ?? "").Trim();

            if (string.IsNullOrWhiteSpace(normalizedKey))
                throw new ArgumentException("Nedostaje AccountName ili AccountKey.", nameof(accountKey));

            return new StorageSharedKeyCredential(normalizedAccount, normalizedKey);
        }

        private static string NormalizeAccountName(string accountName)
        {
            var value = (accountName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Nedostaje AccountName ili AccountKey.", nameof(accountName));

            return value;
        }

        private static string NormalizeContainerName(string containerName)
        {
            var value = (containerName ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Container name je prazan.", nameof(containerName));

            return value;
        }

        private static string NormalizeBlobName(string blobName)
        {
            var value = (blobName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Blob name je prazan.", nameof(blobName));

            return value.Replace('\\', '/');
        }
    }
}