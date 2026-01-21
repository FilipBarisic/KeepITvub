using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;

namespace KeepIT
{
    public partial class LOCALsaveMenu : Window
    {
        private readonly ObservableCollection<FileItem> _files = new();
        private readonly ObservableCollection<FileItem> _archive = new();


        public LOCALsaveMenu()
        {
            InitializeComponent();

            lvFiles.ItemsSource = _files;
            lvArchiveQueue.ItemsSource = _archive;

            btn_Add.Click += btn_Add_Click;
            btn_Remove.Click += btn_Remove_Click;

            lvFiles.SelectionMode = SelectionMode.Extended;
            lvArchiveQueue.SelectionMode = SelectionMode.Extended;

            Loaded += LOCALsaveMenu_Loaded;

            tvFolders.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(TvFolders_Expanded));
            tvFolders.SelectedItemChanged += TvFolders_SelectedItemChanged;
        }

        private void LOCALsaveMenu_Loaded(object sender, RoutedEventArgs e)
        {
            tvFolders.Items.Clear();

            foreach (var d in DriveInfo.GetDrives())
            {
                string header = d.IsReady
                    ? $"{(string.IsNullOrWhiteSpace(d.VolumeLabel) ? "Disk" : d.VolumeLabel)} ({d.Name.TrimEnd('\\')})"
                    : $"(Not ready) ({d.Name.TrimEnd('\\')})"; //-> pokvaren disk ili usb ili slicno sto se nije jos ucitalo ili nece ucitati (inace rusi sa errorom)

                var driveItem = MakeItem(header, d.Name);

                if (d.IsReady) driveItem.Items.Add(MakeDummy());

                tvFolders.Items.Add(driveItem);
            }
        }

        private void TvFolders_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not TreeViewItem item) return;
            if (item.Tag is not string path) return;

            if (item.Items.Count != 1) return;
            if (item.Items[0] is not TreeViewItem dummy) return;
            if (!Equals(dummy.Tag, "DUMMY")) return;

            item.Items.Clear();

            string[] dirs;
            try { dirs = Directory.GetDirectories(path); }
            catch { return; }

            foreach (var dir in dirs)
            {
                var folderItem = MakeItem(System.IO.Path.GetFileName(dir.TrimEnd('\\')), dir);

                try
                {
                    if (Directory.EnumerateDirectories(dir).Any())
                        folderItem.Items.Add(MakeDummy());
                }
                catch
                {
                    // ne dodaj dummy
                }

                item.Items.Add(folderItem);
            }
        }

        private void TvFolders_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tvFolders.SelectedItem is not TreeViewItem item) return;
            if (item.Tag is not string path) return;
            if (!Directory.Exists(path)) { _files.Clear(); return; }

            _files.Clear();

            string[] files;
            try { files = Directory.GetFiles(path); }
            catch { return; }

            foreach (var f in files)
            {
                try
                {
                    var fi = new FileInfo(f);
                    _files.Add(new FileItem
                    {
                        Name = fi.Name,
                        Type = fi.Extension,
                        Size = FormatSize(fi.Length),
                        Modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                        Path = fi.FullName,
                        Folder = System.IO.Path.GetDirectoryName(fi.FullName) ?? ""
                    });

                }
                catch
                {
                    // preskoči fajl ako zapne
                }
            }
        }


        private static TreeViewItem MakeItem(string header, string path) => new TreeViewItem { Header = header, Tag = path };

        private static TreeViewItem MakeDummy() => new TreeViewItem { Header = "...", Tag = "DUMMY" };


        private void btn_Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btn_Back_Click(object sender, RoutedEventArgs e)
        {
            var warn = new BackLocal(
                "Promjene i proces arhiviranja će biti obrisani! Želite li se vratiti na glavni meni?",
                "Upozorenje"
            )
            {
                Owner = this
            };

            var res = warn.ShowDialog();
            if (res != true) return;

            _archive.Clear();

            var main = new MainMenu();
            main.Show();
            this.Close();
        }


        private void btn_Add_Click(object sender, RoutedEventArgs e)
        {
            var selected = lvFiles.SelectedItems.Cast<FileItem>().ToList();
            if (selected.Count == 0) return;

            foreach (var it in selected)
            {
                if (_archive.Any(a => a.Path == it.Path)) continue; // bez duplikata, ponistava stvaranje bezpotrebnih dodavanja ili missclickova
                _archive.Add(it);
            }

            if (_archive.Count > 0)
                lvArchiveQueue.ScrollIntoView(_archive.Last());
        }

        private void btn_Remove_Click(object sender, RoutedEventArgs e)
        {
            var selected = lvArchiveQueue.SelectedItems.Cast<FileItem>().ToList();
            if (selected.Count == 0) return;

            foreach (var it in selected)
                _archive.Remove(it);
        }


        //Formatiranje prepravi sam StackOverflow primjer!!!!!!!!!!!!!!
        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
            return $"{len:0.#} {sizes[order]}";
        }

        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            if (_archive.Count == 0)
            {
                var warn = new ArchiveCount(
                    "Nema datoteka u redu za arhiviranje.",
                    "Upozorenje"
                )
                {
                    Owner = this
                };

                warn.ShowDialog();
                return;
            }


            var dlg = new LocalLocationPick { Owner = this };
            if (dlg.ShowDialog() != true) return;

            string destinationFolder;

            if (dlg.Choice == SaveChoice.CostumSave)
            {
                var ofd = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Odaberi folder za spremanje ZIP-a",
                    Multiselect = false
                };

                if (ofd.ShowDialog(this) != true) return;

                destinationFolder = ofd.FolderName; // <-- bez "string"
            }
            else if (dlg.Choice == SaveChoice.DefaultSave)
            {
                destinationFolder = DefaultArchivePathStore.Get();
            }
            else
            {
                return;
            }

            Directory.CreateDirectory(destinationFolder);

            string zipName = DateTime.Now.ToString("dd.MM.yyyy HH-mm-ss") + ".zip";
            string zipPath = Path.Combine(destinationFolder, zipName);

            using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            foreach (var item in _archive)
            {
                if (!File.Exists(item.Path)) continue;

                zip.CreateEntryFromFile(item.Path, Path.GetFileName(item.Path), CompressionLevel.Optimal);
            }

            var msg = new SaveLocationMsgWin($"Spremljeno je na:\n{zipPath}", "Spremljeno")
            {
                Owner = this
            };
            msg.ShowDialog();
        }


    }
}
