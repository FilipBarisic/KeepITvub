using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace KeepIT
{
    public sealed class AutoArchiveSettings
    {
        public bool LocalEnabled { get; set; }
        public DateTime? LocalSetAtUtc { get; set; }
        public List<string> LocalSources { get; set; } = new();
        public string LocalDestinationFolder { get; set; } = "";
        public int LocalIntervalValue { get; set; } = 1;
        public string LocalIntervalUnit { get; set; } = "Days";
        public DateTime? LocalStartAtUtc { get; set; }
        public DateTime? LocalLastRunUtc { get; set; }
        public DateTime? LocalNextRunUtc { get; set; }
        public string? LocalLastError { get; set; }
        public DateTime? LocalLastErrorUtc { get; set; }

        public bool ServerEnabled { get; set; }
        public DateTime? ServerSetAtUtc { get; set; }
        public List<string> ServerSources { get; set; } = new();
        public string ServerFolderPrefix { get; set; } = "archives/auto/";
        public int ServerIntervalValue { get; set; } = 1;
        public string ServerIntervalUnit { get; set; } = "Days";
        public DateTime? ServerStartAtUtc { get; set; }
        public DateTime? ServerLastRunUtc { get; set; }
        public DateTime? ServerNextRunUtc { get; set; }
        public string? ServerLastError { get; set; }
        public DateTime? ServerLastErrorUtc { get; set; }

        public DateTime? ServerBlockedUntilUtc { get; set; }
    }

    public static class AutoArchiveService
    {
        private static readonly SemaphoreSlim _runLock = new(1, 1);
        private static DispatcherTimer? _pollTimer;

        private static class Units
        {
            public const string Hours = "Hours";
            public const string Days = "Days";
            public const string Weeks = "Weeks";
            public const string Months = "Months";
            public const string Once = "Once";
            public const string Never = "Never";
        }

        private static App? GetApp() => System.Windows.Application.Current as App;

        private static string GetCurrentUserKey()
        {
            var app = GetApp();
            if (app == null || app.CurrentUserId == Guid.Empty)
                return "anonymous";

            return "user-" + app.CurrentUserId.ToString().ToLowerInvariant();
        }

        private static string SettingsFilePath
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KeepIT");

                Directory.CreateDirectory(dir);
                return Path.Combine(dir, $"autoarchive_{GetCurrentUserKey()}.json");
            }
        }

        public static void OnUserChanged()
        {
            Stop();
            Start();
        }

        public static string DefaultLocalDestination()
        {
            var primary = @"C:\AzureKeepIt";
            try
            {
                Directory.CreateDirectory(primary);
                return primary;
            }
            catch
            {
                var fallback = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "AzureKeepIt");

                Directory.CreateDirectory(fallback);
                return fallback;
            }
        }

        public static void Start()
        {
            _pollTimer ??= new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMinutes(1)
            };

            _pollTimer.Tick -= PollTimer_Tick;
            _pollTimer.Tick += PollTimer_Tick;

            if (!_pollTimer.IsEnabled)
                _pollTimer.Start();

            _ = TryRunIfDueAsync();
        }

        public static void Stop()
        {
            if (_pollTimer == null)
                return;

            _pollTimer.Stop();
            _pollTimer.Tick -= PollTimer_Tick;
        }

        private static void PollTimer_Tick(object? sender, EventArgs e) => _ = TryRunIfDueAsync();

        public static AutoArchiveSettings Get()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                    return new AutoArchiveSettings();

                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<AutoArchiveSettings>(json) ?? new AutoArchiveSettings();
            }
            catch
            {
                return new AutoArchiveSettings();
            }
        }

        public static void Set(AutoArchiveSettings settings)
        {
            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(SettingsFilePath, json);
        }

        public static DateTime? ComputeNextRunUtcLocal(AutoArchiveSettings s, DateTime nowUtc) =>
            ComputeNextRunUtc(
                nowUtc,
                s.LocalEnabled,
                s.LocalIntervalValue,
                s.LocalIntervalUnit,
                s.LocalStartAtUtc,
                s.LocalLastRunUtc,
                blockedUntilUtc: null);

        public static DateTime? ComputeNextRunUtcServer(AutoArchiveSettings s, DateTime nowUtc) =>
            ComputeNextRunUtc(
                nowUtc,
                s.ServerEnabled,
                s.ServerIntervalValue,
                s.ServerIntervalUnit,
                s.ServerStartAtUtc,
                s.ServerLastRunUtc,
                s.ServerBlockedUntilUtc);

        private static DateTime? ComputeNextRunUtc(
            DateTime nowUtc,
            bool enabled,
            int intervalValue,
            string intervalUnit,
            DateTime? startAtUtc,
            DateTime? lastRunUtc,
            DateTime? blockedUntilUtc)
        {
            if (!enabled)
                return null;

            var unit = string.IsNullOrWhiteSpace(intervalUnit) ? Units.Days : intervalUnit;
            var value = Math.Max(1, intervalValue);

            if (blockedUntilUtc.HasValue && blockedUntilUtc.Value > nowUtc)
                return blockedUntilUtc.Value;

            if (unit == Units.Never)
                return null;

            if (unit == Units.Once)
            {
                if (lastRunUtc != null)
                    return null;

                return startAtUtc ?? nowUtc;
            }

            if (startAtUtc.HasValue && lastRunUtc == null)
                return startAtUtc.Value;

            if (lastRunUtc == null)
                return nowUtc;

            return AddInterval(lastRunUtc.Value, value, unit);
        }

        private static bool IsDue(bool enabled, DateTime? nextRunUtc, DateTime nowUtc) =>
            enabled && nextRunUtc.HasValue && nextRunUtc.Value <= nowUtc;

        private static void RefreshNextRunTimes(AutoArchiveSettings s, DateTime nowUtc)
        {
            s.LocalNextRunUtc = ComputeNextRunUtcLocal(s, nowUtc);
            s.ServerNextRunUtc = ComputeNextRunUtcServer(s, nowUtc);
        }

        public static async Task TryRunIfDueAsync()
        {
            var settings = Get();
            var nowUtc = DateTime.UtcNow;

            RefreshNextRunTimes(settings, nowUtc);
            Set(settings);

            var localDue = IsDue(settings.LocalEnabled, settings.LocalNextRunUtc, nowUtc);
            var serverDue = IsDue(settings.ServerEnabled, settings.ServerNextRunUtc, nowUtc);

            if (!localDue && !serverDue)
                return;

            await _runLock.WaitAsync().ConfigureAwait(false);
            try
            {
                settings = Get();
                nowUtc = DateTime.UtcNow;

                RefreshNextRunTimes(settings, nowUtc);

                localDue = IsDue(settings.LocalEnabled, settings.LocalNextRunUtc, nowUtc);
                serverDue = IsDue(settings.ServerEnabled, settings.ServerNextRunUtc, nowUtc);

                if (localDue)
                    await RunLocalAsync(settings).ConfigureAwait(false);

                if (serverDue)
                    await RunServerAsync(settings).ConfigureAwait(false);

                nowUtc = DateTime.UtcNow;
                RefreshNextRunTimes(settings, nowUtc);
                Set(settings);
            }
            finally
            {
                _runLock.Release();
            }
        }

        private static async Task RunLocalAsync(AutoArchiveSettings s)
        {
            try
            {
                var sources = (s.LocalSources ?? new())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (sources.Count == 0)
                    return;

                var destination = string.IsNullOrWhiteSpace(s.LocalDestinationFolder)
                    ? DefaultLocalDestination()
                    : s.LocalDestinationFolder;

                Directory.CreateDirectory(destination);

                var tempZip = await CreateTempZipAsync("LocalAuto", sources).ConfigureAwait(false);

                var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var zipName = $"LocalAuto_{stamp}.zip";
                var outputPath = EnsureUniquePath(Path.Combine(destination, zipName));

                File.Copy(tempZip, outputPath, overwrite: false);
                TryDeleteFile(tempZip);

                s.LocalLastRunUtc = DateTime.UtcNow;
                s.LocalLastError = null;
                s.LocalLastErrorUtc = null;

                if ((s.LocalIntervalUnit ?? Units.Days) == Units.Once)
                    s.LocalEnabled = false;
            }
            catch (Exception ex)
            {
                s.LocalLastError = ex.Message;
                s.LocalLastErrorUtc = DateTime.UtcNow;
                s.LocalLastRunUtc = DateTime.UtcNow;

                if ((s.LocalIntervalUnit ?? Units.Days) == Units.Once)
                    s.LocalEnabled = false;
            }
        }

        private static async Task RunServerAsync(AutoArchiveSettings s)
        {
            var app = GetApp();

            if (app == null || app.CurrentUserId == Guid.Empty)
            {
                s.ServerBlockedUntilUtc = DateTime.UtcNow.AddMinutes(2);
                return;
            }

            var sas = app.CurrentContainerSasUri;
            if (sas == null)
            {
                s.ServerBlockedUntilUtc = DateTime.UtcNow.AddMinutes(2);
                return;
            }

            try
            {
                var sources = (s.ServerSources ?? new())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (sources.Count == 0)
                    return;

                s.ServerBlockedUntilUtc = null;

                var tempZip = await CreateTempZipAsync("ServerAuto", sources).ConfigureAwait(false);

                var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var zipName = $"ServerAuto_{stamp}.zip";

                var folderPrefix = NormalizeBlobPrefix(s.ServerFolderPrefix);
                var blobName = folderPrefix + zipName;

                await AzureBlobHelper
                    .UploadZipToUserContainerAsync(sas, tempZip, blobName)
                    .ConfigureAwait(false);

                TryDeleteFile(tempZip);

                s.ServerLastRunUtc = DateTime.UtcNow;
                s.ServerLastError = null;
                s.ServerLastErrorUtc = null;

                if ((s.ServerIntervalUnit ?? Units.Days) == Units.Once)
                    s.ServerEnabled = false;
            }
            catch (Exception ex)
            {
                s.ServerLastError = ex.Message;
                s.ServerLastErrorUtc = DateTime.UtcNow;
                s.ServerLastRunUtc = DateTime.UtcNow;

                if ((s.ServerIntervalUnit ?? Units.Days) == Units.Once)
                    s.ServerEnabled = false;
            }
        }

        private static Task<string> CreateTempZipAsync(string prefix, List<string> sources)
        {
            return Task.Run(() =>
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "KeepIT");
                Directory.CreateDirectory(tempDir);

                var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var zipPath = Path.Combine(tempDir, $"{prefix}_{stamp}.zip");

                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);

                foreach (var source in sources)
                {
                    if (File.Exists(source))
                    {
                        if (included.Add(source))
                            TryAddFile(zip, source);

                        continue;
                    }

                    if (!Directory.Exists(source))
                        continue;

                    IEnumerable<string> files;
                    try
                    {
                        files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories);
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var file in files)
                    {
                        if (!File.Exists(file))
                            continue;

                        if (!included.Add(file))
                            continue;

                        TryAddFile(zip, file);
                    }
                }

                return zipPath;
            });
        }

        private static void TryAddFile(ZipArchive zip, string fullPath)
        {
            try
            {
                var entryName = MakeZipEntryName(fullPath);
                zip.CreateEntryFromFile(fullPath, entryName, CompressionLevel.Optimal);
            }
            catch
            {
            }
        }

        private static string MakeZipEntryName(string fullPath)
        {
            var s = fullPath.Replace(':', '_').TrimStart('\\', '/');
            return s.Replace('\\', '/');
        }

        private static DateTime AddInterval(DateTime fromUtc, int value, string unit)
        {
            var v = Math.Max(1, value);

            return unit switch
            {
                Units.Hours => fromUtc.AddHours(v),
                Units.Days => fromUtc.AddDays(v),
                Units.Weeks => fromUtc.AddDays(7 * v),
                Units.Months => fromUtc.AddMonths(v),
                _ => fromUtc.AddDays(v)
            };
        }

        private static string EnsureUniquePath(string path)
        {
            if (!File.Exists(path))
                return path;

            var dir = Path.GetDirectoryName(path)!;
            var name = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);

            for (var i = 1; ; i++)
            {
                var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
                if (!File.Exists(candidate))
                    return candidate;
            }
        }

        private static string NormalizeBlobPrefix(string prefix)
        {
            var p = (prefix ?? "").Trim();
            if (p.Length == 0)
                return "";

            p = p.Replace('\\', '/');
            return p.EndsWith("/") ? p : p + "/";
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }
    }
}