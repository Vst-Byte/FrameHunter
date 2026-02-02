using System.Windows;

namespace FrameHunterFPS
{
    public partial class ExitDialog : Window
    {
        // Propriedade para saber o que o usuário escolheu
        // 0 = Cancelar, 1 = Sair Totalmente, 2 = Minimizar Tray
        public int UserChoice { get; private set; } = 0;

        public ExitDialog()
        {
            InitializeComponent();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = 1; // Exit
            this.Close();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = 2; // Background
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = 0; // Cancel
            this.Close();
        }
    }
}