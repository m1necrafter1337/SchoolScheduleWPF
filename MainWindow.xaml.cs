using SchoolSchedule.Views;
using System;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace SchoolSchedule
{
    public partial class MainWindow : Window
    {
        private readonly SchedulePage _schedulePage = new SchedulePage();
        private readonly ReferenceDataPage _referencePage = new ReferenceDataPage();
        private SettingsPage _settingsPage;

        public MainWindow()
        {
            InitializeComponent();
            _settingsPage = new SettingsPage(ApplyTheme);
            Navigate(0);
        }

        private void NavBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int idx))
                Navigate(idx);
        }

        private void Navigate(int idx)
        {
            MainFrame.Navigate(idx switch
            {
                1 => (object)_referencePage,
                2 => _settingsPage,
                _ => _schedulePage
            });

            foreach (UIElement child in NavPanel.Children)
            {
                if (child is Button b)
                    b.FontWeight = b.Tag?.ToString() == idx.ToString()
                        ? FontWeights.Bold
                        : FontWeights.Normal;
            }
        }

        private void ApplyTheme(string theme)
        {
            var dict = new ResourceDictionary();
            dict.Source = theme == "Dark"
                ? new Uri("pack://application:,,,/Themes/DarkTheme.xaml")
                : new Uri("pack://application:,,,/Themes/LightTheme.xaml");

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);

            // Переподключаем стили после смены темы
            var styles = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Styles/Styles.xaml")
            };
            Application.Current.Resources.MergedDictionaries.Add(styles);
        }
    }
}
