using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KeepIT
{
    public partial class LOCALsaveMenu : Window
    {
        private const string DummyTag = "DUMMY";

        private readonly ObservableCollection<FileItem> _folderFiles = new();
        private readonly ObservableCollection<FileItem> _archiveQueue = new();

        public LOCALsaveMenu()
        {
            InitializeComponent();

            FolderFilesListView.ItemsSource = _folderFiles;
            ArchiveQueueListView.ItemsSource = _archiveQueue;

            FolderFilesListView.SelectionMode = SelectionMode.Extended;
            ArchiveQueueListView.SelectionMode = SelectionMode.Extended;

            Loaded += LocalSaveMenu_Loaded;

            FoldersTreeView.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(FoldersTreeView_Expanded));
            FoldersTreeView.SelectedItemChanged += FoldersTreeView_SelectedItemChanged;
        }

        private void LocalSaveMenu_Loaded(object sender, RoutedEventArgs e) { UcitajDiskove(); }

        // Učitava diskove i prikazuje ih u TreeView-u.
        // Svaki disk koji je spreman dobiva dummy child item.
        private void UcitajDiskove()
        {
            FoldersTreeView.Items.Clear();

            foreach (var drive in DriveInfo.GetDrives())
            {
                var header = drive.IsReady
                    ? $"{(string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Disk" : drive.VolumeLabel)} ({drive.Name.TrimEnd('\\')})"
                    : $"(Not ready) ({drive.Name.TrimEnd('\\')})";

                var driveItem = CreateTreeItem(header, drive.Name);

                if (drive.IsReady)
                    driveItem.Items.Add(CreateDummyItem());

                FoldersTreeView.Items.Add(driveItem);
            }
        }

        // Učitava stvarne podmape i direktorije
        private void FoldersTreeView_Expanded(object sender, RoutedEventArgs e)
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
        private void FoldersTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (FoldersTreeView.SelectedItem is not TreeViewItem item)
                return;

            if (item.Tag is not string path)
                return;

            RefreshFilesForFolder(path);
        }
        private static TreeViewItem CreateTreeItem(string header, string path) => new TreeViewItem { Header = header, Tag = path };
        private static TreeViewItem CreateDummyItem() => new TreeViewItem { Header = "...", Tag = DummyTag };

        // Prikazuje datoteke u mapi i podake o njima.
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
                    var fileInfo = new FileInfo(filePath);

                    _folderFiles.Add(new FileItem
                    {
                        Name = fileInfo.Name,
                        Type = fileInfo.Extension,
                        Size = FormatBytes(fileInfo.Length),
                        Modified = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                        Path = fileInfo.FullName,
                        Folder = Path.GetDirectoryName(fileInfo.FullName) ?? string.Empty
                    });
                }
                catch
                {
                }
            }
        }

        private void AddToQueueButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = FolderFilesListView.SelectedItems.Cast<FileItem>().ToList();
            if (selected.Count == 0)
                return;

            foreach (var item in selected)
            {
                if (_archiveQueue.Any(a => a.Path == item.Path))
                    continue;

                _archiveQueue.Add(item);
            }

            if (_archiveQueue.Count > 0)
                ArchiveQueueListView.ScrollIntoView(_archiveQueue.Last());
        }
        private void RemoveFromQueueButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = ArchiveQueueListView.SelectedItems.Cast<FileItem>().ToList();
            if (selected.Count == 0)
                return;

            foreach (var item in selected)
                _archiveQueue.Remove(item);
        }

        // Formatira byteove.
        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            int unitIndex = 0;

            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                unitIndex++;
                value /= 1024;
            }

            return $"{value:0.#} {units[unitIndex]}";
        }

        // Arhivira datoteke u redu u ZIP datoteku na odabranoj lokaciji.
        private void ArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_archiveQueue.Count == 0)
            {
                Alerts.Show(this, "Nema datoteka u redu za arhiviranje.", "Upozorenje");
                return;
            }

            var dialog = new LocalLocationPick { Owner = this };
            if (dialog.ShowDialog() != true)
                return;

            string destinationFolder;

            if (dialog.Choice == SaveChoice.CostumSave)
            {
                var folderDialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Odaberi folder za spremanje ZIP-a",
                    Multiselect = false
                };

                if (folderDialog.ShowDialog(this) != true)
                    return;

                destinationFolder = folderDialog.FolderName;
            }
            else if (dialog.Choice == SaveChoice.DefaultSave)
            {
                destinationFolder = DefaultArchivePathStore.Get();
            }
            else
            {
                return;
            }

            Directory.CreateDirectory(destinationFolder);

            var zipName = DateTime.Now.ToString("dd.MM.yyyy HH-mm-ss") + ".zip";
            var zipPath = Path.Combine(destinationFolder, zipName);

            using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);

            foreach (var item in _archiveQueue)
            {
                if (!File.Exists(item.Path))
                    continue;

                zip.CreateEntryFromFile(item.Path, Path.GetFileName(item.Path), CompressionLevel.Optimal);
            }

            Alerts.Show(this, $"Spremljeno je na:\n{zipPath}", "Spremljeno");
        }

        //UI gumbovi za miniziranje, povratak, i pomicanje prozora.
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }
        private void TopBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) { DragMove(); } }
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

            _archiveQueue.Clear();

            var main = new MainMenu();
            main.Show();
            Close();
        }
    }
}