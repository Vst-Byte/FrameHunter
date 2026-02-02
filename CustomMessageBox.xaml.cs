using System.Windows;

namespace FrameHunterFPS
{
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox(string title, string message)
        {
            InitializeComponent();
            TxtTitle.Text = title.ToUpper(); // Título em Maiúsculo
            TxtMessage.Text = message;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Fecha a janela ao clicar
        }
    }
}