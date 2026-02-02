using System;
using System.Windows.Controls;
using System.Windows.Threading;
using MediaBrushes = System.Windows.Media.Brushes;

namespace FrameHunterFPS
{
    public partial class AppMenuPage : Page
    {
        private DispatcherTimer? _timer;
        private bool _isUpdating = false;

        public AppMenuPage()
        {
            InitializeComponent();
            HardwareService.Start();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;
            try
            {
                await HardwareService.Update();
                if (TxtCpu != null)
                {
                    // Foco em Uso % para evitar o "Reading..." da temperatura
                    TxtCpu.Text = $"{HardwareService.CpuLoad}%";
                    PbCpu.Value = HardwareService.CpuLoad;
                    TxtCpuName.Text = HardwareService.CpuName;
                    TxtCpuClock.Text = HardwareService.CpuClock;
                }
                if (TxtGpu != null)
                {
                    TxtGpu.Text = $"{HardwareService.GpuTemp}°C";
                    PbGpu.Value = HardwareService.GpuTemp;
                    TxtGpuName.Text = HardwareService.GpuName;
                    TxtGpuClock.Text = HardwareService.GpuClock;
                }
                if (TxtRam != null)
                {
                    TxtRam.Text = $"{HardwareService.RamLoad}%";
                    PbRam.Value = HardwareService.RamLoad;
                    TxtRamDetail.Text = $"{HardwareService.RamUsed} / {HardwareService.RamTotal}";
                    TxtRamClock.Text = HardwareService.RamClock;
                }
                if (TxtPing != null)
                {
                    TxtPing.Text = $"{HardwareService.PingMs} ms";
                    PbPing.Value = Math.Min(HardwareService.PingMs, 200);
                }
            }
            catch { }
            finally { _isUpdating = false; }
        }
    }
}