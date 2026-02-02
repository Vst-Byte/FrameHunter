using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

// --- CORREÇÃO COMPLETA DE AMBIGUIDADE ---
// Adicionei Brushes e ColorConverter que estavam faltando
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
// ----------------------------------------

namespace FrameHunterFPS
{
    public partial class GpuTuningPage : Page
    {
        public GpuTuningPage()
        {
            InitializeComponent();
            DetectAndSetup();
        }

        private void DetectAndSetup()
        {
            // 1. Detecção Automática do Hardware
            string gpu = GpuHelper.DetectGpuBrand();

            TxtDetectedGpu.Text = gpu;

            // Definição de Cores
            var nvidiaColor = (Color)ColorConverter.ConvertFromString("#76B900");
            var amdColor = (Color)ColorConverter.ConvertFromString("#FF4545");

            // 2. Lógica de Visibilidade
            if (gpu == "NVIDIA")
            {
                TxtDetectedGpu.Foreground = new SolidColorBrush(nvidiaColor);
                PanelNvidia.Visibility = Visibility.Visible;
                PanelAmd.Visibility = Visibility.Collapsed;
            }
            else if (gpu == "AMD")
            {
                TxtDetectedGpu.Foreground = new SolidColorBrush(amdColor);
                PanelAmd.Visibility = Visibility.Visible;
                PanelNvidia.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Fallback
                TxtDetectedGpu.Text = "UNKNOWN / INTEL";
                TxtDetectedGpu.Foreground = Brushes.Gray; // Agora isso funciona sem erro

                PanelNvidia.Visibility = Visibility.Visible;
                PanelAmd.Visibility = Visibility.Collapsed;
            }
        }

        // ==========================================
        // BOTÕES NVIDIA
        // ==========================================
        private void BtnNvidiaProfile_Click(object sender, RoutedEventArgs e)
        {
            bool success = GpuHelper.ApplyNvidiaProfile();
            if (success)
                new CustomMessageBox("PROFILE LOADED", "Nvidia Profile imported successfully via Inspector.").ShowDialog();
            else
                new CustomMessageBox("ERROR", "Could not find 'nvidiaProfileInspector.exe' resources.").ShowDialog();
        }

        private void BtnNvidiaTweak_Click(object sender, RoutedEventArgs e)
        {
            GpuHelper.ApplyNvidiaTweaks();
            new CustomMessageBox("APPLIED", "Power Management tweak applied to Registry.").ShowDialog();
        }

        private void BtnNvidiaTelemetry_Click(object sender, RoutedEventArgs e)
        {
            GpuHelper.DisableNvidiaTelemetry();
            new CustomMessageBox("TELEMETRY DISABLED", "Nvidia background services have been disabled to save CPU.").ShowDialog();
        }

        private void BtnNvidiaMsi_Click(object sender, RoutedEventArgs e)
        {
            GpuHelper.EnableNvidiaMSI();
            new CustomMessageBox("MSI MODE ENABLED", "Message Signaled Interrupts enabled.\nA restart is required for lower latency.").ShowDialog();
        }

        // ==========================================
        // BOTÕES AMD
        // ==========================================
        private void BtnAmdUlps_Click(object sender, RoutedEventArgs e)
        {
            GpuHelper.DisableAmdUlps();
            new CustomMessageBox("ULPS DISABLED", "Ultra Low Power State disabled.\nRestart required.").ShowDialog();
        }

        private void BtnAmdCache_Click(object sender, RoutedEventArgs e)
        {
            GpuHelper.ResetAmdShaderCache();
            new CustomMessageBox("CACHE CLEARED", "DirectX Shader Cache cleared successfully.").ShowDialog();
        }

        private void BtnAmdMpo_Click(object sender, RoutedEventArgs e)
        {
            GpuHelper.DisableAmdMPO();
            new CustomMessageBox("MPO DISABLED", "Multi-Plane Overlay disabled to fix flickering.").ShowDialog();
        }

        private void BtnAmdDeepSleep_Click(object sender, RoutedEventArgs e)
        {
            GpuHelper.DisableAmdDeepSleep();
            new CustomMessageBox("CLOCKS FORCED", "Deep Sleep disabled for consistent performance.").ShowDialog();
        }

        // ==========================================
        // BOTÕES UNIVERSAIS
        // ==========================================
        private void BtnPriority_Click(object sender, RoutedEventArgs e)
        {
            GpuHelper.SetGpuPriority(true);
            new CustomMessageBox("PRIORITY SET", "Windows GPU Priority set to 'High' for games.").ShowDialog();
        }

        private void BtnHags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
                new CustomMessageBox("HAGS ENABLED", "Hardware Accelerated GPU Scheduling enabled.\nPlease restart your PC.").ShowDialog();
            }
            catch
            {
                new CustomMessageBox("ERROR", "Could not apply settings. Run App as Administrator.").ShowDialog();
            }
        }
    }
}