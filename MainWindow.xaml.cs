using SchoolSchedule.Views;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SchoolSchedule.Services;

namespace SchoolSchedule
{
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private readonly SchedulePage _schedulePage = new SchedulePage();
        private readonly ReferenceDataPage _referencePage = new ReferenceDataPage();
        private SettingsPage? _settingsPage;

        public MainWindow()
        {
            InitializeComponent();
            ThemeService.ThemeChanged += ApplyTitleBarTheme;
            Loaded += (_, __) =>
            {
                ApplyTitleBarTheme();
                Navigate(0); // открываем расписание по умолчанию
            };
        }

        // -- Тёмный titlebar ------------------------------------------

        private void ApplyTitleBarTheme()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;
            int isDark = ThemeService.IsDark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref isDark, sizeof(int));
        }

        // -- Системные кнопки -----------------------------------------

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        // -- Навигация ------------------------------------------------

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
                2 => _settingsPage ??= new SettingsPage(ApplyTheme),
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

        // -- Смена темы -----------------------------------------------

        private void ApplyTheme(string theme)
        {
            var dict = new ResourceDictionary
            {
                Source = theme == "Dark"
                    ? new Uri("pack://application:,,,/Themes/DarkTheme.xaml")
                    : new Uri("pack://application:,,,/Themes/LightTheme.xaml")
            };

            var merged = Application.Current.Resources.MergedDictionaries;
            merged.Clear();
            merged.Add(dict);

            var styles = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Styles/Styles.xaml")
            };
            merged.Add(styles);
        }
    }
}
