using System.Windows;

namespace FrameHunterFPS
{
    public partial class CustomRestockMessageBox : Window
    {
        public bool IsConfirmed { get; private set; }

        public CustomRestockMessageBox(string title, string message)
        {
            InitializeComponent();
            txtTitle.Text = title;
            txtMessage.Text = message;
            IsConfirmed = false;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            this.Close();
        }
    }
}