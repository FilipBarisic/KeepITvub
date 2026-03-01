using System;
using System.Windows;
using System.Windows.Input;

namespace KeepIT
{
    public enum AlertResult
    {
        Closed = 0,
        Primary = 1,
        Secondary = 2,
        Tertiary = 3
    }

    public partial class Alerts : Window
    {
        private AlertResult _result = AlertResult.Closed;

        public Alerts(string message, string title = "ALERT")
        {
            InitializeComponent();
            txtTitle.Text = string.IsNullOrWhiteSpace(title) ? "ALERT" : title;
            txtMessage.Text = message ?? string.Empty;
        }

        // 1) OK-only (info/warn/error)
        public static void Show(Window owner, string message, string title = "ALERT", string okText = "OK")
        {
            var w = new Alerts(message, title)
            {
                Owner = owner,
                ShowInTaskbar = false
            };

            w.SetupButtons(primaryText: okText, secondaryText: null, tertiaryText: null);
            w.ShowDialog();
        }

        // 2) OK / Cancel (confirm)
        public static bool Confirm(Window owner, string message, string title = "POTVRDA", string okText = "OK", string cancelText = "CANCEL")
        {
            var w = new Alerts(message, title)
            {
                Owner = owner,
                ShowInTaskbar = false
            };

            w.SetupButtons(primaryText: okText, secondaryText: cancelText, tertiaryText: null);
            w.ShowDialog();

            return w._result == AlertResult.Primary;
        }

        // 3) Two actions + Cancel (npr. Logout / Logout+Exit / Odustani)
        public static AlertResult Choose(Window owner, string message, string title,
                                             string primaryText, string secondaryText, string cancelText = "ODUSTANI")
        {
            var w = new Alerts(message, title)
            {
                Owner = owner,
                ShowInTaskbar = false
            };

            w.SetupButtons(primaryText: primaryText, secondaryText: secondaryText, tertiaryText: cancelText);
            w.ShowDialog();

            return w._result;
        }

        private void SetupButtons(string primaryText, string? secondaryText, string? tertiaryText)
        {
            btnPrimary.Content = primaryText;

            if (!string.IsNullOrWhiteSpace(secondaryText))
            {
                btnSecondary.Content = secondaryText;
                btnSecondary.Visibility = Visibility.Visible;
            }
            else btnSecondary.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(tertiaryText))
            {
                btnTertiary.Content = tertiaryText;
                btnTertiary.Visibility = Visibility.Visible;
            }
            else btnTertiary.Visibility = Visibility.Collapsed;

            Loaded += (_, __) => btnPrimary.Focus();
        }

        private void btnPrimary_Click(object sender, RoutedEventArgs e)
        {
            _result = AlertResult.Primary;
            Close();
        }

        private void btnSecondary_Click(object sender, RoutedEventArgs e)
        {
            _result = AlertResult.Secondary;
            Close();
        }

        private void btnTertiary_Click(object sender, RoutedEventArgs e)
        {
            _result = AlertResult.Tertiary;
            Close();
        }

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Enter -> Primary
            if (e.Key == Key.Enter)
            {
                _result = AlertResult.Primary;
                Close();
                return;
            }

            // Esc -> Cancel ako postoji, inače Primary
            if (e.Key == Key.Escape)
            {
                if (btnTertiary.Visibility == Visibility.Visible)
                    _result = AlertResult.Tertiary;
                else if (btnSecondary.Visibility == Visibility.Visible)
                    _result = AlertResult.Secondary;
                else
                    _result = AlertResult.Primary;

                Close();
            }
        }
    }
}