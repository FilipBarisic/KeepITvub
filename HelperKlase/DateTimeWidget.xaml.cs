using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace KeepIT
{
    public partial class DateTimeWidget : UserControl
    {
        private readonly DispatcherTimer _timer;

        public DateTimeWidget()
        {
            InitializeComponent();

            UpdateDateTime();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timer.Tick += (s, e) => UpdateDateTime();
            _timer.Start();

            // Za svaki slučaj kad se control ukloni
            Unloaded += (s, e) => _timer.Stop();
        }

        private void UpdateDateTime()
        {
            var now = DateTime.Now;
            txtTime.Text = now.ToString("HH:mm:ss");
            txtDate.Text = now.ToString("dd.MM.yyyy");
        }
    }
}
