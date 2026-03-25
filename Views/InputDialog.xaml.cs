using System.Windows;
using System.Windows.Input;

namespace SchoolSchedule.Views
{
    public partial class InputDialog : Window
    {
        public string Result { get; private set; } = string.Empty;

        public InputDialog(string title, string prompt)
        {
            InitializeComponent();
            Title = title;
            Prompt.Text = prompt;
            Loaded += (_, __) => InputBox.Focus();
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            Result = InputBox.Text;
            DialogResult = true;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { Result = InputBox.Text; DialogResult = true; }
            if (e.Key == Key.Escape) DialogResult = false;
        }
    }
}
