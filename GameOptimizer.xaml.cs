using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace FrameHunterFPS
{
    public partial class GameOptimizer : Page
    {
        public ObservableCollection<GameEntry> DetectedGames { get; set; } = new ObservableCollection<GameEntry>();

        private readonly string[] CompetitiveGames = {
            "Counter-Strike", "League of Legends", "VALORANT", "Dota 2",
            "Apex Legends", "Rainbow Six Siege", "Overwatch",
            "Rocket League", "Warzone", "Dead by Daylight"
        };

        public GameOptimizer()
        {
            InitializeComponent();

            // CORREÇÃO 1: Vincula à 'GamesList' (a lista lateral do novo layout)
            GamesList.ItemsSource = DetectedGames;

            this.Loaded += async (s, e) =>
            {
                if (DetectedGames.Count == 0)
                {
                    await RunAutoScan();
                    // Seleciona o primeiro jogo automaticamente para não ficar vazio
                    if (DetectedGames.Count > 0) GamesList.SelectedIndex = 0;
                }
            };
        }

        private async Task RunAutoScan()
        {
            DetectedGames.Clear();
            var found = await Task.Run(() => GameHelper.GetAllGames());
            foreach (var game in found)
            {
                game.ShowInjectButton = CompetitiveGames.Any(c => game.Name.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0);
                DetectedGames.Add(game);
                _ = Task.Run(async () => {
                    var path = await GameHelper.GetGameCoverUrl(game.Name);
                    if (!string.IsNullOrEmpty(path))
                        System.Windows.Application.Current.Dispatcher.Invoke(() => game.ImageUrl = path);
                });
            }
        }

        // Botão "ENABLE OPTIMIZATION" (Toggle Principal)
        private void BtnOptimizeGame_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Primitives.ToggleButton;
            var currentGame = btn?.Tag as GameEntry;

            if (currentGame != null)
            {
                if (btn.IsChecked == true)
                {
                    // Ativa a flag IsExpanded (que agora serve como "Ativado")
                    currentGame.IsExpanded = true;
                    ApplySystemOptimizations(currentGame);
                }
                else
                {
                    currentGame.IsExpanded = false;
                }
            }
        }

        // Aplica otimizações no Registro do Windows
        private void ApplySystemOptimizations(GameEntry game)
        {
            try
            {
                string exePath = GetMainExePath(game.InstallLocation);
                if (string.IsNullOrEmpty(exePath)) return;

                // 1. GPU High Perf
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences"))
                {
                    key.SetValue(exePath, "GpuPreference=2;");
                }

                // 2. Disable Fullscreen Opt
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))
                {
                    key.SetValue(exePath, "~ DISABLEDXMAXIMIZEDWINDOWEDMODE");
                }
            }
            catch { }
        }

        private void BtnInjectConfig_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var game = btn?.Tag as GameEntry;

            if (game != null)
            {
                try
                {
                    string targetPath = GetConfigPath(game.Name);

                    if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
                    {
                        // CORREÇÃO 2: Uso explícito de System.Windows.MessageBox para evitar ambiguidade
                        System.Windows.MessageBox.Show($"Arquivo de configuração não encontrado.\nCaminho esperado: {targetPath}", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string configData = "";
                    if (game.Name.Contains("League of Legends")) configData = "[Performance]\nShadowsEnabled=0\nWaitForVerticalSync=0\nEnableHUDAnimations=0\nFrameCapType=2";
                    else if (game.Name.Contains("VALORANT")) configData = "[ScalabilityGroups]\nsg.ResolutionQuality=100\nsg.ViewDistanceQuality=0\nsg.AntiAliasingQuality=0\nsg.ShadowQuality=0\nsg.PostProcessQuality=0";
                    else if (game.Name.Contains("Dead by Daylight")) configData = "[/Script/Engine.RendererSettings]\nr.DefaultFeature.MotionBlur=0\nr.DefaultFeature.Bloom=0\nr.ShadowQuality=0";
                    else if (game.Name.Contains("Warzone") || game.Name.Contains("Call of Duty")) configData = "// Generated by FrameHunter\nsetcl -1836731280 \"0.0\"";

                    // Backup e Injeção
                    File.Copy(targetPath, targetPath + ".bak", true);
                    File.AppendAllText(targetPath, "\n\n" + configData);

                    System.Windows.MessageBox.Show($"Configuração aplicada com sucesso em:\n{Path.GetFileName(targetPath)}", "FrameHunter", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Erro na injeção: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GpuSwitch_Click(object sender, RoutedEventArgs e)
        {
            var cb = sender as System.Windows.Controls.CheckBox;
            var game = cb?.Tag as GameEntry;
            if (game != null && cb.IsChecked == true)
            {
                string exePath = GetMainExePath(game.InstallLocation);
                if (!string.IsNullOrEmpty(exePath)) ForceHighPerformanceGPU(exePath);
            }
        }

        private void ForceHighPerformanceGPU(string exePath)
        {
            try
            {
                string script = $"New-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\DirectX\\UserGpuPreferences' -Name '{exePath}' -Value 'GpuPreference=2;' -PropertyType String -Force";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{script}\"",
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });
            }
            catch { }
        }

        // Helpers
        private string GetMainExePath(string installDir)
        {
            try
            {
                var exes = Directory.GetFiles(installDir, "*.exe", SearchOption.AllDirectories);
                return exes.FirstOrDefault(x => x.Contains("Shipping")) ?? exes.FirstOrDefault() ?? "";
            }
            catch { return ""; }
        }

        private string GetConfigPath(string gameName)
        {
            string doc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (gameName.Contains("League of Legends")) return @"C:\Riot Games\League of Legends\Config\game.cfg";
            if (gameName.Contains("VALORANT"))
            {
                string baseVal = Path.Combine(localApp, "VALORANT", "Saved", "Config");
                if (Directory.Exists(baseVal))
                {
                    var dirs = Directory.GetDirectories(baseVal);
                    if (dirs.Length > 0) return Path.Combine(dirs[0], "Windows", "GameUserSettings.ini");
                }
            }
            if (gameName.Contains("Dead by Daylight")) return Path.Combine(localApp, "DeadByDaylight", "Saved", "Config", "WindowsNoEditor", "GameUserSettings.ini");
            if (gameName.Contains("Warzone")) return Path.Combine(doc, "Call of Duty", "players", "options.3.cod22.cst");

            return null;
        }
    }
}