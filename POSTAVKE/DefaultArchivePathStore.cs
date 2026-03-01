using System.IO;

namespace KeepIT
{
    public static class DefaultArchivePathStore
    {
        public const string FallbackPath = @"C:\KeepIT_Archive";

        private static readonly string SettingsDirectory =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KeepIT");

        private static readonly string SettingsFilePath =
            Path.Combine(SettingsDirectory, "default_archive_path.txt");

        public static string Get()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                    return FallbackPath;

                var value = (File.ReadAllText(SettingsFilePath) ?? string.Empty).Trim();
                return string.IsNullOrWhiteSpace(value) ? FallbackPath : value;
            }
            catch
            {
                return FallbackPath;
            }
        }

        public static void Set(string newPath)
        {
            var value = (newPath ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
                return;

            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                File.WriteAllText(SettingsFilePath, value);
            }
            catch
            {
            }
        }
    }
}