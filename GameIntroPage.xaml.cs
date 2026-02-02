using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

// --- CORREÇÃO DE AMBIGUIDADE (WPF vs WinForms) ---
// Isso diz ao código exatamente qual biblioteca usar
using Point = System.Windows.Point;
using Application = System.Windows.Application;
// -------------------------------------------------

namespace FrameHunterFPS
{
    public partial class GameIntroPage : Page
    {
        public GameIntroPage()
        {
            InitializeComponent();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            // Lógica de Animação (Zoom & Fade)
            this.RenderTransformOrigin = new Point(0.5, 0.5);
            ScaleTransform scaleParams = new ScaleTransform();
            this.RenderTransform = scaleParams;

            DoubleAnimation zoomIn = new DoubleAnimation(1.0, 1.15, TimeSpan.FromSeconds(0.5));
            DoubleAnimation fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(0.4));

            // Quando a animação terminar, troca de página
            fadeOut.Completed += (s, _) =>
            {
                // Acessa a MainWindow principal
                // O "Application" aqui agora funciona por causa do 'using' que adicionamos lá em cima
                if (Application.Current.MainWindow is MainWindow mainWin)
                {
                    // Chama o método público para ir ao Dashboard
                    mainWin.GoToDashboard();
                }
            };

            scaleParams.BeginAnimation(ScaleTransform.ScaleXProperty, zoomIn);
            scaleParams.BeginAnimation(ScaleTransform.ScaleYProperty, zoomIn);
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}