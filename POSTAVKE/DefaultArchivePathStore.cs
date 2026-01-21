using System.IO;

namespace KeepIT
{
    public static class DefaultArchivePathStore
    {
        public const string FallbackPath = @"C:\KeepIT_Archive"; // za nedaj Boze

        private static string SettingsFilePath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KeepIT",
                "default_archive_path.txt"
            );

        public static string Get()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var v = File.ReadAllText(SettingsFilePath).Trim();
                    if (!string.IsNullOrWhiteSpace(v))
                        return v;
                }
            }
            catch
            {
            
            }

            return FallbackPath;
        }

        public static void Set(string newPath)
        {
            if (string.IsNullOrWhiteSpace(newPath))
            {
                return;
            }

            var dir = Path.GetDirectoryName(SettingsFilePath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(SettingsFilePath, newPath);
        }
    }
}
