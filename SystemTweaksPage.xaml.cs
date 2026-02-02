using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
// Resolve ambiguidades críticas entre WPF e WinForms
using CheckBox = System.Windows.Controls.CheckBox;

namespace FrameHunterFPS
{
    public partial class SystemTweaksPage : Page
    {
        public SystemTweaksPage()
        {
            InitializeComponent();
        }

        // Permite marcar a opção clicando em qualquer lugar do card
        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                var checkBox = FindVisualChild<CheckBox>(border);
                if (checkBox != null)
                {
                    checkBox.IsChecked = !checkBox.IsChecked;
                }
            }
        }

        // --- MÉTODO UNDO ATUALIZADO COM A NOVA CAIXA PERSONALIZADA ---
        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            // Usa a sua nova CustomRestockMessageBox em vez da padrão do Windows
            var msg = new CustomRestockMessageBox("RESTORE STOCK", "Are you sure you want to restore all Windows default settings?\n\nThis will undo all optimizations.");
            msg.Owner = Window.GetWindow(this);
            msg.ShowDialog();

            if (msg.IsConfirmed)
            {
                // Chama a função de restauração no Helper
                OptimizationHelper.RestoreAllDefaults();

                // Desmarca visualmente todos os CheckBoxes
                ResetAllCheckBoxes(false);

                // Mostra mensagem de sucesso com o novo design
                new CustomRestockMessageBox("SUCCESS", "All settings have been reverted to original Windows defaults.\n\nPlease restart your computer.").ShowDialog();
            }
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            // Verificação de segurança nos componentes principais
            bool allChecked = (cbGameMode?.IsChecked == true) && (cbHAGS?.IsChecked == true);
            bool newState = !allChecked;

            ResetAllCheckBoxes(newState);

            BtnSelectAll.Content = newState ? "DESELECT ALL" : "SELECT ALL";
        }

        // Método centralizado para gerenciar os CheckBoxes
        private void ResetAllCheckBoxes(bool newState)
        {
            if (cbPower != null) cbPower.IsChecked = newState;
            if (cbCpu != null) cbCpu.IsChecked = newState;
            if (cbFastStartup != null) cbFastStartup.IsChecked = newState;
            if (cbHibernation != null) cbHibernation.IsChecked = newState;
            if (cbUSBPower != null) cbUSBPower.IsChecked = newState;
            if (cbGameMode != null) cbGameMode.IsChecked = newState;
            if (cbHAGS != null) cbHAGS.IsChecked = newState;
            if (cbVBS != null) cbVBS.IsChecked = newState;
            if (cbTimer != null) cbTimer.IsChecked = newState;
            if (cbMouse != null) cbMouse.IsChecked = newState;
            if (cbBloat != null) cbBloat.IsChecked = newState;
            if (cbTelemetry != null) cbTelemetry.IsChecked = newState;
            if (cbVisualFX != null) cbVisualFX.IsChecked = newState;
            if (cbTransparency != null) cbTransparency.IsChecked = newState;
            if (cbGameBar != null) cbGameBar.IsChecked = newState;
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            // Aplica as otimizações conforme a seleção
            if (cbPower?.IsChecked == true) OptimizationHelper.ApplyUltimatePerformance();

            OptimizationHelper.ApplyCpuUnpark(cbCpu?.IsChecked == true);
            OptimizationHelper.ApplyFastStartup(cbFastStartup?.IsChecked == true);
            OptimizationHelper.ApplyHibernation(cbHibernation?.IsChecked == true);
            OptimizationHelper.ApplyUSBPower(cbUSBPower?.IsChecked == true);
            OptimizationHelper.ApplyGameMode(cbGameMode?.IsChecked == true);
            OptimizationHelper.ApplyHAGS(cbHAGS?.IsChecked == true);
            OptimizationHelper.ApplyVBS(cbVBS?.IsChecked == true);
            OptimizationHelper.ApplyTimerResolution(cbTimer?.IsChecked == true);
            OptimizationHelper.ApplyMouseAccel(cbMouse?.IsChecked == true);
            OptimizationHelper.ApplyBloatwareRemover(cbBloat?.IsChecked == true);
            OptimizationHelper.ApplyTelemetry(cbTelemetry?.IsChecked == true);
            OptimizationHelper.ApplyVisualFX(cbVisualFX?.IsChecked == true);
            OptimizationHelper.ApplyTransparency(cbTransparency?.IsChecked == true);
            OptimizationHelper.ApplyGameBar(cbGameBar?.IsChecked == true);

            // Mensagem final de sucesso
            new CustomRestockMessageBox("SYSTEM OPTIMIZED", "The selected tweaks have been applied successfully.\n\nA system restart is required.").ShowDialog();
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild) return typedChild;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }
    }
}