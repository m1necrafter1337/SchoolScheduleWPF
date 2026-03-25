using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace SchoolSchedule.Views
{
    public partial class SelectionDialog : Window
    {
        public string? SelectedItem { get; private set; }

        public SelectionDialog(string title, string prompt, IEnumerable<string> items)
        {
            InitializeComponent();
            Title = title;
            Prompt.Text = prompt;
            foreach (var item in items)
                ItemsList.Items.Add(item);
            if (ItemsList.Items.Count > 0)
                ItemsList.SelectedIndex = 0;
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsList.SelectedItem == null) return;
            SelectedItem = ItemsList.SelectedItem.ToString();
            DialogResult = true;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ItemsList.SelectedItem == null) return;
            SelectedItem = ItemsList.SelectedItem.ToString();
            DialogResult = true;
        }
    }
}
