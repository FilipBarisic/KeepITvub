using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KeepIT
{
    public partial class SERVERsaveMenu : Window
    {
        private const string DummyTag = "DUMMY";
        private const string DefaultDownloadFolder = @"C:\AzureKeepIt"; // Može se promijeniti, ali neka default lokacija za preuzimanje arhiva sa servera. Nema opciju kroz aplikaciju za promjenu.

        private readonly ObservableCollection<FileItem> _folderFiles = new();
        private readonly ObservableCollection<FileItem> _uploadQueue = new();
        private readonly ObservableCollection<FileItem> _downloadQueue = new();

        //Ne može nasljeđivati, koristi se samo kao struktura za držanje informacija o blobovima.
        private sealed class ServerBlobInfo
        {
            public string BlobName { get; init; } = "";
            public string FileName { get; init; } = "";
            public long SizeBytes { get; init; }
        }

        private bool IsDownloadMode => borderServerPullView.Visibility == Visibility.Visible;

        public SERVERsaveMenu()
        {
            InitializeComponent();

            lvFiles.ItemsSource = _folderFiles;
            lvArchiveQueue.ItemsSource = _uploadQueue;
            lvServerPullQueue.ItemsSource = _downloadQueue;

            Loaded += Window_Loaded;

            tvFolders.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(FoldersTree_Expanded));
            tvFolders.SelectedItemChanged += FoldersTree_SelectedItemChanged;

            UploadMode();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) { UcitajDiskove(); }

        private void UcitajDiskove()
        {
            tvFolders.Items.Clear();

            foreach (var drive in DriveInfo.GetDrives())
            {
                var header = drive.IsReady
                    ? $"{(string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Disk" : drive.VolumeLabel)} ({drive.Name.TrimEnd('\\')})"
                    : $"(Not ready) ({drive.Name.TrimEnd('\\')})";

                var driveItem = CreateTreeItem(header, drive.Name);

                if (drive.IsReady)
                    driveItem.Items.Add(CreateDummyItem());

                tvFolders.Items.Add(driveItem);
            }
        }

        private void FoldersTree_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not TreeViewItem item)
                return;

            if (item.Tag is not string path)
                return;

            if (item.Items.Count != 1 || item.Items[0] is not TreeViewItem dummy || !Equals(dummy.Tag, DummyTag))
                return;

            item.Items.Clear();

            string[] directories;
            try
            {
                directories = Directory.GetDirectories(path);
            }
            catch
            {
                return;
            }

            foreach (var dir in directories)
            {
                var folderItem = CreateTreeItem(Path.GetFileName(dir.TrimEnd('\\')), dir);

                try
                {
                    if (Directory.EnumerateDirectories(dir).Any())
                        folderItem.Items.Add(CreateDummyItem());
                }
                catch
                {
                }

                item.Items.Add(folderItem);
            }
        }
        private void FoldersTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tvFolders.SelectedItem is not TreeViewItem item)
                return;

            if (item.Tag is not string path)
                return;

            RefreshFilesForFolder(path);
        }
        private void RefreshFilesForFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                _folderFiles.Clear();
                return;
            }

            _folderFiles.Clear();

            string[] files;
            try
            {
                files = Directory.GetFiles(folderPath);
            }
            catch
            {
                return;
            }

            foreach (var filePath in files)
            {
                try
                {
                    var info = new FileInfo(filePath);

                    _folderFiles.Add(new FileItem
                    {
                        Name = info.Name,
                        Type = info.Extension,
                        Size = FormatSize(info.Length),
                        Modified = info.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                        Path = info.FullName,
                        Folder = Path.GetDirectoryName(info.FullName) ?? string.Empty
                    });
                }
                catch
                {
                }
            }
        }

        // Pomoćne funkcije za kreiranje TreeViewItema i dummy itema.
        private static TreeViewItem CreateTreeItem(string header, object tag) => new TreeViewItem { Header = header, Tag = tag };
        private static TreeViewItem CreateDummyItem() => new TreeViewItem { Header = "...", Tag = DummyTag };


        private void UploadMode()
        {
            btn_PovuciServerSave.Visibility = Visibility.Collapsed;
            btn_ServerSave.Visibility = Visibility.Collapsed;

            btn_Save.Visibility = Visibility.Visible;
            btn_Add.Visibility = Visibility.Visible;
            btn_Remove.Visibility = Visibility.Visible;
            btn_ServerPull.Visibility = Visibility.Visible;

            borderServerPicker.Visibility = Visibility.Visible;
            borderServerPullView.Visibility = Visibility.Collapsed;
        }
        private void DownloadMode()
        {
            btn_ServerPull.Visibility = Visibility.Collapsed;
            btn_Save.Visibility = Visibility.Collapsed;

            btn_ServerSave.Visibility = Visibility.Visible;
            btn_PovuciServerSave.Visibility = Visibility.Visible;

            btn_Add.Visibility = Visibility.Visible;
            btn_Remove.Visibility = Visibility.Visible;

            borderServerPicker.Visibility = Visibility.Collapsed;
            borderServerPullView.Visibility = Visibility.Visible;
        }


        // Buttoni za akcije na prozoru.
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsDownloadMode)
            {
                if (tvServerData.SelectedItem is not TreeViewItem tvi)
                    return;

                if (tvi.Tag is not ServerBlobInfo info)
                    return;

                if (_downloadQueue.Any(x => x.Path == info.BlobName))
                    return;

                _downloadQueue.Add(new FileItem
                {
                    Name = info.FileName,
                    Size = FormatSize(info.SizeBytes),
                    Path = info.BlobName
                });

                if (_downloadQueue.Count > 0)
                    lvServerPullQueue.ScrollIntoView(_downloadQueue.Last());

                return;
            }

            var selected = lvFiles.SelectedItems.Cast<FileItem>().ToList();
            if (selected.Count == 0)
                return;

            foreach (var it in selected)
            {
                if (_uploadQueue.Any(a => a.Path == it.Path))
                    continue;

                _uploadQueue.Add(it);
            }

            if (_uploadQueue.Count > 0)
                lvArchiveQueue.ScrollIntoView(_uploadQueue.Last());
        }
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsDownloadMode)
            {
                var selected = lvServerPullQueue.SelectedItems.Cast<FileItem>().ToList();
                if (selected.Count == 0)
                    return;

                foreach (var it in selected)
                    _downloadQueue.Remove(it);

                return;
            }

            var selectedLocal = lvArchiveQueue.SelectedItems.Cast<FileItem>().ToList();
            if (selectedLocal.Count == 0)
                return;

            foreach (var it in selectedLocal)
                _uploadQueue.Remove(it);
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var confirmed = Alerts.Confirm(
                this,
                "Promjene i proces arhiviranja će biti obrisani! Želite li se vratiti na glavni meni?",
                "Upozorenje",
                okText: "OK",
                cancelText: "CANCEL");

            if (!confirmed)
                return;

            _uploadQueue.Clear();

            new MainMenu().Show();
            Close();
        }
        private void TopBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }


        //Mora biti async zbog ponovnog učitavanja arhiva nakon povlačenja, da se vidi novi red u TreeViewu.
        //Upload mode ne mora biti async jer se nakon uploada ne radi refresh TreeViewa.
        private async void DownloadMode_Click(object sender, RoutedEventArgs e)
        {
            DownloadMode();
            _downloadQueue.Clear();
            await ServerArhiva();
        }
        private void UploadMode_Click(object sender, RoutedEventArgs e)
        {
            _downloadQueue.Clear();
            tvServerData.Items.Clear();
            UploadMode();
        }
        
        // Povlači sve arhive za koje korisnik ima pristup.
        private async Task ServerArhiva()
        {
            var app = (App)Application.Current;
            if (app.CurrentContainerSasUri == null)
            {
                Alerts.Show(this, "Sesija nije inicijalizirana (SAS). Prijavi se ponovno.", "GREŠKA");
                return;
            }

            tvServerData.Items.Clear();

            try
            {
                var blobs = await AzureBlobHelper.ListBlobsAsync(app.CurrentContainerSasUri, "archives/");

                var zipBlobs = blobs
                    .Where(b => b.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(b => b.Name)
                    .ToList();

                var root = new TreeViewItem { Header = "archives", IsExpanded = true };

                if (zipBlobs.Count == 0)
                {
                    root.Items.Add(new TreeViewItem { Header = "(Nema ZIP arhiva)", IsEnabled = false });
                    tvServerData.Items.Add(root);
                    return;
                }

                foreach (var b in zipBlobs)
                {
                    var fileName = Path.GetFileName(b.Name);
                    var sizeBytes = b.Properties.ContentLength ?? 0;

                    root.Items.Add(new TreeViewItem
                    {
                        Header = fileName,
                        Tag = new ServerBlobInfo
                        {
                            BlobName = b.Name,
                            FileName = fileName,
                            SizeBytes = sizeBytes
                        }
                    });
                }

                tvServerData.Items.Add(root);
            }
            catch (Exception ex)
            {
                Alerts.Show(this, "Greška pri dohvaćanju podataka sa servera:\n" + ex.Message, "GREŠKA");
            }
        }

        //Serve download i upload rade sa povlačenjem arhiva sa servera ili spremanjem arhiva na računalo.
        private async void ServerDownload_Click(object sender, RoutedEventArgs e)
        {
            if (_downloadQueue.Count == 0)
            {
                Alerts.Show(this, "Nema odabranih ZIP arhiva za povlačenje.", "SERVER PULL");
                return;
            }

            var app = (App)Application.Current;
            if (app.CurrentContainerSasUri == null)
            {
                Alerts.Show(this, "Sesija nije inicijalizirana (SAS). Prijavi se ponovno.", "GREŠKA");
                return;
            }

            var originalText = btn_PovuciServerSave.Content?.ToString() ?? "Povuci sa servera";

            try
            {
                btn_PovuciServerSave.IsEnabled = false;
                btn_PovuciServerSave.Content = "Preuzimam...";

                Directory.CreateDirectory(DefaultDownloadFolder);

                var downloaded = 0;

                foreach (var it in _downloadQueue.ToList())
                {
                    if (string.IsNullOrWhiteSpace(it.Path))
                        continue;

                    var blobName = it.Path;
                    var fileName = Path.GetFileName(blobName);
                    var localPath = UkloniDuplikate(Path.Combine(DefaultDownloadFolder, fileName));

                    await AzureBlobHelper.DownloadBlobToFileAsync(app.CurrentContainerSasUri, blobName, localPath);
                    downloaded++;
                }

                Alerts.Show(this, $"Preuzeto: {downloaded}\nLokacija: {DefaultDownloadFolder}", "SERVER PULL");
            }
            catch (Exception ex)
            {
                Alerts.Show(this, "Greška pri povlačenju sa servera:\n" + ex.Message, "GREŠKA");
            }
            finally
            {
                btn_PovuciServerSave.Content = originalText;
                btn_PovuciServerSave.IsEnabled = true;
            }
        }
        private async void ServerUpload_Click(object sender, RoutedEventArgs e)
        {
            if (_uploadQueue.Count == 0)
            {
                Alerts.Show(this, "Nema odabranih datoteka za spremanje na server.", "SERVER SAVE");
                return;
            }

            var app = (App)Application.Current;
            if (app.CurrentContainerSasUri == null || string.IsNullOrWhiteSpace(app.CurrentUsername))
            {
                Alerts.Show(this, "Sesija nije inicijalizirana (SAS/korisnik). Prijavi se ponovno.", "GREŠKA");
                return;
            }

            var zipFileName = BuildZipName(app.CurrentUsername);
            var tempDir = Path.Combine(Path.GetTempPath(), "KeepIT");
            Directory.CreateDirectory(tempDir);

            var zipPath = Path.Combine(tempDir, zipFileName);
            var originalText = btn_Save.Content?.ToString() ?? "Spremi na server";

            try
            {
                btn_Save.IsEnabled = false;
                btn_Save.Content = "Spremanje...";

                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    foreach (var it in _uploadQueue.ToList())
                    {
                        if (string.IsNullOrWhiteSpace(it.Path) || !File.Exists(it.Path))
                            continue;

                        var entryName = BuildZipPath(it.Path);
                        zip.CreateEntryFromFile(it.Path, entryName, CompressionLevel.Optimal);
                    }
                }

                var blobName = $"archives/{zipFileName}";
                await AzureBlobHelper.UploadZipToUserContainerAsync(app.CurrentContainerSasUri, zipPath, blobName);

                _uploadQueue.Clear();

                Alerts.Show(this, $"Uspješno spremljeno na server:\n{zipFileName}", "SERVER SAVE");
            }
            catch (Exception ex)
            {
                Alerts.Show(this, "Greška pri spremanju na server:\n" + ex.Message, "GREŠKA");
            }
            finally
            {
                btn_Save.Content = originalText;
                btn_Save.IsEnabled = true;

                try
                {
                    if (File.Exists(zipPath))
                        File.Delete(zipPath);
                }
                catch
                {
                }
            }
        }

        //UkloniDuplikate, UrediFileName, BuildZipName, BuildZipPath su pomoćne funkcije koje služe za:
        //- UkloniDuplikate: Ako već postoji datoteka sa istim imenom u folderu za preuzimanje, dodaje (1), (2) itd. na kraj imena da se ne bi prebrisala.
        //- UrediFileName: Uklanja nedozvoljene znakove iz imena korisnika da se ne bi stvarale neispravne datoteke.
        //- BuildZipName: Stvara ime ZIP datoteke na temelju trenutnog datuma i imena korisnika.
        //- BuildZipPath: Pretvara apsolutnu putanju datoteke u relativnu putanju unutar ZIP arhive, zamjenjujući dvotočke i početne kose crte.
        private static string UkloniDuplikate(string path)
        {
            if (!File.Exists(path))
                return path;

            var dir = Path.GetDirectoryName(path) ?? "";
            var name = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);

            for (var i = 1; ; i++)
            {
                var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
                if (!File.Exists(candidate))
                    return candidate;
            }
        }
        private static string UrediFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(name.Where(ch => !invalid.Contains(ch)).ToArray()).Trim();
        }
        private static string BuildZipName(string username)
        {
            var safeUser = UrediFileName(username);
            var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return $"{stamp}_{safeUser}.zip";
        }
        private static string BuildZipPath(string fullPath)
        {
            var s = fullPath.Replace(':', '_').TrimStart('\\', '/');
            return s.Replace('\\', '/');
        }

        // FormatSize pretvara veličinu u bajtovima u čitljiv format sa odgovarajućom jedinicom (B, KB, MB, GB, TB).
        private static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            var unitIndex = 0;

            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                unitIndex++;
                value /= 1024;
            }

            return $"{value:0.#} {units[unitIndex]}";
        }
    }
}