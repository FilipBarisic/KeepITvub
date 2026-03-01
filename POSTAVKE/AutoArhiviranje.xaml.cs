using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KeepIT
{
    public partial class AutoArhiviranje : Window
    {
        private readonly ObservableCollection<string> _localSourcePaths = new();
        private readonly ObservableCollection<string> _serverSourcePaths = new();
        private bool _isLoaded;

        public AutoArhiviranje()
        {
            InitializeComponent();
            Loaded += AutoArhiviranje_Loaded;
        }
        private void AutoArhiviranje_Loaded(object sender, RoutedEventArgs e)
        {
            LocalSourcesListBox.ItemsSource = _localSourcePaths;
            ServerSourcesListBox.ItemsSource = _serverSourcePaths;

            LocalIntervalUnitComboBox.SelectedIndex = 1;
            ServerIntervalUnitComboBox.SelectedIndex = 1;

            var settings = AutoArchiveService.Get();
            UcitajPostavke(settings);

            var app = (App)Application.Current;
            ServerLoginText.Text = app.IsLoggedIn ? $"Server: {app.CurrentUsername}" : "Server: nije prijavljen";

            _isLoaded = true;
            UpdateStatus();
        }

        // Postavi i ukloni automatizaciju za local i server.
        private void PostaviLocalAuto_Click(object sender, RoutedEventArgs e)
        {
            var settings = AutoArchiveService.Get();
            ReadLocalFromUi(settings);

            if (!ProvjeriLocalIzvore(settings))
                return;

            if (!NikadJednomOdabir(settings.LocalIntervalUnit, "Lokalna"))
                return;

            settings.LocalEnabled = LocalEnabledCheckBox.IsChecked == true;
            settings.LocalSetAtUtc = DateTime.UtcNow;

            AutoArchiveService.Set(settings);
            AutoArchiveService.Start();

            UpdateStatus();
            Alerts.Show(this, "Lokalna automatizacija je postavljena.", "AUTO");
        }
        private void UkloniLocalAuto_Click(object sender, RoutedEventArgs e)
        {
            var settings = AutoArchiveService.Get();
            settings.LocalEnabled = false;
            settings.LocalNextRunUtc = null;

            AutoArchiveService.Set(settings);
            UpdateStatus();

            Alerts.Show(this, "Lokalna automatizacija je uklonjena.", "AUTO");
        }
        private void PostaviServerAuto_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            if (app.CurrentContainerSasUri == null)
            {
                Alerts.Show(this, "Nisi prijavljen ili nema SAS pristupa. Prijavi se pa pokušaj ponovno.", "SERVER");
                return;
            }

            var settings = AutoArchiveService.Get();
            ReadServerFromUi(settings);

            if (!ProvjeriServerIzvore(settings))
                return;

            if (!NikadJednomOdabir(settings.ServerIntervalUnit, "Server"))
                return;

            settings.ServerEnabled = ServerEnabledCheckBox.IsChecked == true;
            settings.ServerSetAtUtc = DateTime.UtcNow;

            AutoArchiveService.Set(settings);
            AutoArchiveService.Start();

            UpdateStatus();
            Alerts.Show(this, "Server automatizacija je postavljena.", "AUTO");
        }
        private void UkloniServerAuto_Click(object sender, RoutedEventArgs e)
        {
            var settings = AutoArchiveService.Get();
            settings.ServerEnabled = false;
            settings.ServerNextRunUtc = null;

            AutoArchiveService.Set(settings);
            UpdateStatus();

            Alerts.Show(this, "Server automatizacija je uklonjena.", "AUTO");
        }
        
        //Izvor opcije za local i server.
        private void DodajFolderIzvorLOCAL_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Multiselect = false, Title = "Odaberi folder (lokalno)" };
            if (dialog.ShowDialog(this) != true)
                return;

            DodajLokaniIzvor(dialog.FolderName);
        }
        private void DodajFileIzvorLOCAL_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Multiselect = true, Title = "Odaberi fileove (lokalno)" };
            if (dialog.ShowDialog(this) != true)
                return;

            foreach (var filePath in dialog.FileNames)
                DodajLokaniIzvor(filePath);
        }
        private void UkloniLocalIzvore_Click(object sender, RoutedEventArgs e)
        {
            var selected = LocalSourcesListBox.SelectedItems.Cast<string>().ToList();
            foreach (var path in selected)
                _localSourcePaths.Remove(path);

            LokalniIzvorSpremi();
            UpdateStatus();
        }
        private void DodajFolderIzvorSERVER_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Multiselect = false, Title = "Odaberi folder (server)" };
            if (dialog.ShowDialog(this) != true)
                return;

            DodajServerIzvor(dialog.FolderName);
        }
        private void DodajFileIzvorSERVER_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Multiselect = true, Title = "Odaberi fileove (server)" };
            if (dialog.ShowDialog(this) != true)
                return;

            foreach (var filePath in dialog.FileNames)
                DodajServerIzvor(filePath);
        }
        private void UkloniServerIzvore_Click(object sender, RoutedEventArgs e)
        {
            var selected = ServerSourcesListBox.SelectedItems.Cast<string>().ToList();
            foreach (var path in selected)
                _serverSourcePaths.Remove(path);

            ServerIzvorSpremi();
            UpdateStatus();
        }

        // Omogući korisniku da odabere folder gdje će se spremati lokalne arhive preko file explorera.
        private void FileExZaOdabir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Multiselect = false, Title = "Odaberi lokalnu destinaciju" };
            if (dialog.ShowDialog(this) != true)
                return;

            LocalDestinationTextBox.Text = dialog.FolderName;
            UpdateStatus();
        }

        // Dodaje izvor u listu ako nije dodan. Pod izvor se smatra i folder i file, sve što korisnik odabere.
        private void DodajLokaniIzvor(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (_localSourcePaths.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase)))
                return;

            _localSourcePaths.Add(path);
            LokalniIzvorSpremi();
            UpdateStatus();
        }
        private void DodajServerIzvor(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (_serverSourcePaths.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase)))
                return;

            _serverSourcePaths.Add(path);
            ServerIzvorSpremi();
            UpdateStatus();
        }

        // Sprema u settings putanje izvora.
        private void LokalniIzvorSpremi()
        {
            var settings = AutoArchiveService.Get();
            settings.LocalSources = _localSourcePaths.ToList();
            AutoArchiveService.Set(settings);
        }
        private void ServerIzvorSpremi()
        {
            var settings = AutoArchiveService.Get();
            settings.ServerSources = _serverSourcePaths.ToList();
            AutoArchiveService.Set(settings);
        }

        // Učita iz settings postavlja u UI, ali ne dira disk
        private void UcitajPostavke(AutoArchiveSettings settings)
        {
            LocalEnabledCheckBox.IsChecked = settings.LocalEnabled;
            ServerEnabledCheckBox.IsChecked = settings.ServerEnabled;

            LocalIntervalValueTextBox.Text = Math.Max(1, settings.LocalIntervalValue).ToString(CultureInfo.InvariantCulture);
            ServerIntervalValueTextBox.Text = Math.Max(1, settings.ServerIntervalValue).ToString(CultureInfo.InvariantCulture);

            OdaberiInterval(LocalIntervalUnitComboBox, settings.LocalIntervalUnit);
            OdaberiInterval(ServerIntervalUnitComboBox, settings.ServerIntervalUnit);

            LocalStartDatePicker.SelectedDate = settings.LocalStartAtUtc?.ToLocalTime().Date;
            ServerStartDatePicker.SelectedDate = settings.ServerStartAtUtc?.ToLocalTime().Date;

            LocalDestinationTextBox.Text = string.IsNullOrWhiteSpace(settings.LocalDestinationFolder)
                ? AutoArchiveService.DefaultLocalDestination()
                : settings.LocalDestinationFolder;

            ServerFolderTextBox.Text = string.IsNullOrWhiteSpace(settings.ServerFolderPrefix)
                ? "archives/auto/"
                : settings.ServerFolderPrefix;

            _localSourcePaths.Clear();
            foreach (var p in (settings.LocalSources ?? new()).Distinct(StringComparer.OrdinalIgnoreCase))
                _localSourcePaths.Add(p);

            _serverSourcePaths.Clear();
            foreach (var p in (settings.ServerSources ?? new()).Distinct(StringComparer.OrdinalIgnoreCase))
                _serverSourcePaths.Add(p);
        }

        // Odabire dan ako ikoji drugi ili niti jedan nije odabran, inače odabire ono što je u settingsima
        private static void OdaberiInterval(ComboBox comboBox, string? unit)
        {
            unit ??= "Days";

            foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
            {
                if ((item.Tag?.ToString() ?? "Days") == unit)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }

            comboBox.SelectedIndex = 1;
        }

        // Čita postavke iz UI-a i upisuje u settings objekt, ali ne sprema na disk za SERVER i LOCAL
        private void ReadLocalFromUi(AutoArchiveSettings settings)
        {
            settings.LocalEnabled = LocalEnabledCheckBox.IsChecked == true;
            settings.LocalIntervalValue = ParseInt(LocalIntervalValueTextBox.Text, 1);

            var unit = (LocalIntervalUnitComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            settings.LocalIntervalUnit = string.IsNullOrWhiteSpace(unit) ? "Days" : unit;

            var date = LocalStartDatePicker.SelectedDate;
            settings.LocalStartAtUtc = date.HasValue
                ? DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Local).ToUniversalTime()
                : null;

            settings.LocalDestinationFolder = (LocalDestinationTextBox.Text ?? "").Trim();
            settings.LocalSources = _localSourcePaths.ToList();
            settings.LocalNextRunUtc = AutoArchiveService.ComputeNextRunUtcLocal(settings, DateTime.UtcNow);
        }
        private void ReadServerFromUi(AutoArchiveSettings settings)
        {
            settings.ServerEnabled = ServerEnabledCheckBox.IsChecked == true;
            settings.ServerIntervalValue = ParseInt(ServerIntervalValueTextBox.Text, 1);

            var unit = (ServerIntervalUnitComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            settings.ServerIntervalUnit = string.IsNullOrWhiteSpace(unit) ? "Days" : unit;

            var date = ServerStartDatePicker.SelectedDate;
            settings.ServerStartAtUtc = date.HasValue
                ? DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Local).ToUniversalTime()
                : null;

            settings.ServerFolderPrefix = (ServerFolderTextBox.Text ?? "").Trim();
            settings.ServerSources = _serverSourcePaths.ToList();
            settings.ServerNextRunUtc = AutoArchiveService.ComputeNextRunUtcServer(settings, DateTime.UtcNow);
        }

        // Provjerava da li su uneseni izvori i destinacija
        private bool ProvjeriLocalIzvore(AutoArchiveSettings settings)
        {
            if (!_localSourcePaths.Any())
            {
                Alerts.Show(this, "Lokalno: dodaj barem jedan izvor.", "AUTO");
                return false;
            }

            if (string.IsNullOrWhiteSpace(LocalDestinationTextBox.Text))
            {
                Alerts.Show(this, "Lokalno: odaberi lokaciju spremanja.", "AUTO");
                return false;
            }

            return true;
        }
        private bool ProvjeriServerIzvore(AutoArchiveSettings settings)
        {
            if (!_serverSourcePaths.Any())
            {
                Alerts.Show(this, "Server: dodaj barem jedan izvor.", "AUTO");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ServerFolderTextBox.Text))
            {
                Alerts.Show(this, "Server: upiši folder u containeru (npr. archives/auto/).", "AUTO");
                return false;
            }

            return true;
        }
        
        private bool NikadJednomOdabir(string? unit, string label)
        {
            unit = string.IsNullOrWhiteSpace(unit) ? "Days" : unit;

            if (unit == "Never")
            {
                return Alerts.Confirm(
                    this,
                    $"Odabrano je 'Nikad'. {label} automatizacija se neće izvršavati.\nŽeliš li svejedno spremiti?",
                    "UPOZORENJE",
                    "SPREMI",
                    "ODUSTANI");
            }

            if (unit == "Once")
            {
                return Alerts.Confirm(
                    this,
                    $"Odabrano je 'Jednom'. {label} automatizacija će se izvršiti samo jednom.\nŽeliš li spremiti?",
                    "UPOZORENJE",
                    "SPREMI",
                    "ODUSTANI");
            }

            return true;
        }

        // Ako korisnik unese nesto sto nije broj, ili broj manji od 1, vrati se default vrijednost
        private static int ParseInt(string? text, int fallback)
        {
            if (int.TryParse((text ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                return Math.Max(1, n);

            return fallback;
        }

        //Formatira vrijeme u lokalno
        //UpdateStatus čita postavke, izračuna sljedeće vrijeme izvršavanja i prikaže sve statuse i greške za local i server
        private static string FormatirajVrijeme(DateTime? utc)
        {
            if (!utc.HasValue)
                return "-";

            return utc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }
        private void UpdateStatus()
        {
            if (!_isLoaded)
                return;

            var settings = AutoArchiveService.Get();
            var nowUtc = DateTime.UtcNow;

            settings.LocalNextRunUtc = AutoArchiveService.ComputeNextRunUtcLocal(settings, nowUtc);
            settings.ServerNextRunUtc = AutoArchiveService.ComputeNextRunUtcServer(settings, nowUtc);
            AutoArchiveService.Set(settings);

            //LOCAL statusi
            LocalSetText.Text = "Postavljeno: " + FormatirajVrijeme(settings.LocalSetAtUtc);
            LocalLastRunText.Text = "Zadnje izvršeno: " + FormatirajVrijeme(settings.LocalLastRunUtc);
            LocalNextRunText.Text = "Sljedeće: " + FormatirajVrijeme(settings.LocalNextRunUtc);
            LocalErrorText.Text = string.IsNullOrWhiteSpace(settings.LocalLastError)
                ? "Greška: -"
                : "Greška: (" + FormatirajVrijeme(settings.LocalLastErrorUtc) + ") " + settings.LocalLastError;

            //SERVER statusi
            ServerSetText.Text = "Postavljeno: " + FormatirajVrijeme(settings.ServerSetAtUtc);
            ServerLastRunText.Text = "Zadnje izvršeno: " + FormatirajVrijeme(settings.ServerLastRunUtc);
            ServerNextRunText.Text = "Sljedeće: " + FormatirajVrijeme(settings.ServerNextRunUtc);
            ServerErrorText.Text = string.IsNullOrWhiteSpace(settings.ServerLastError)
                ? "Greška: -"
                : "Greška: (" + FormatirajVrijeme(settings.ServerLastErrorUtc) + ") " + settings.ServerLastError;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void TopBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            new PostavkeMenu().Show();
            Close();
        }
    }
}