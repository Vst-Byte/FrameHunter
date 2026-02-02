using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;

// --- APELIDOS PARA EVITAR CONFLITOS (CRUCIAL) ---
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;
// ------------------------------------------------

// APELIDOS PARA CONFLITOS COMUNS (Padrão do projeto)
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using CheckBox = System.Windows.Controls.CheckBox;
using Point = System.Windows.Point;

namespace FrameHunterFPS
{
    public partial class MainWindow : Window
    {
        private WinForms.NotifyIcon _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            InitializeSystemTray();
        }

        private void InitializeSystemTray()
        {
            _notifyIcon = new WinForms.NotifyIcon();
            try
            {
                _notifyIcon.Icon = Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetEntryAssembly().Location);
            }
            catch
            {
                _notifyIcon.Icon = Drawing.SystemIcons.Application;
            }
            _notifyIcon.Visible = false;
            _notifyIcon.Text = "FrameHunter FPS Booster";
            _notifyIcon.DoubleClick += (s, args) => ShowWindowFromTray();

            var contextMenu = new WinForms.ContextMenuStrip();
            contextMenu.Items.Add("Abrir", null, (s, e) => ShowWindowFromTray());
            contextMenu.Items.Add("Sair", null, (s, e) => ForceExit());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new GameIntroPage());
            if (SidebarBorder != null) SidebarBorder.Visibility = Visibility.Collapsed;
            if (ColMenu != null) ColMenu.Width = new GridLength(0);
            Grid.SetColumn(MainFrame, 0);
            Grid.SetColumnSpan(MainFrame, 2);
        }

        private void HandleCloseRequest()
        {
            ExitDialog dialog = new ExitDialog();
            dialog.Owner = this;
            dialog.ShowDialog();

            if (dialog.UserChoice == 1) ForceExit();
            else if (dialog.UserChoice == 2) MinimizeToTray();
        }

        private void MinimizeToTray()
        {
            this.Hide();
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(3000, "FrameHunter", "Rodando em 2º plano.", WinForms.ToolTipIcon.Info);
        }

        private void ShowWindowFromTray()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            _notifyIcon.Visible = false;
        }

        private void ForceExit()
        {
            if (_notifyIcon != null) _notifyIcon.Dispose();
            // AQUI usamos o Application do WPF explicitamente
            System.Windows.Application.Current.Shutdown();
        }

        // Eventos
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => HandleCloseRequest();
        private void BtnExit_Click(object sender, RoutedEventArgs e) => HandleCloseRequest();

        public void GoToDashboard()
        {
            if (SidebarBorder != null) SidebarBorder.Visibility = Visibility.Visible;
            if (ColMenu != null) ColMenu.Width = new GridLength(260);
            Grid.SetColumn(MainFrame, 1);
            Grid.SetColumnSpan(MainFrame, 1);
            MainFrame.Navigate(new AppMenuPage());
        }

        // Navegação
        private void BtnOverview_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new AppMenuPage());
        private void BtnWindows_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new SystemTweaksPage());
        private void BtnGpu_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new GpuTuningPage());

        // AQUI ESTÁ A MUDANÇA: Navega para a nova página "GameOptimizer"
        private void BtnGames_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new GameOptimizer());

        // Placeholders
        private void BtnLatency_Click(object sender, RoutedEventArgs e) { }
        private void BtnCrosshair_Click(object sender, RoutedEventArgs e) { }
        private void BtnOverlay_Click(object sender, RoutedEventArgs e) { }
        private void BtnClean_Click(object sender, RoutedEventArgs e) { }
        private void BtnNetwork_Click(object sender, RoutedEventArgs e) { }
        private void BtnStartup_Click(object sender, RoutedEventArgs e) { }
    }
}