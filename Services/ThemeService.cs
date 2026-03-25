using System;
using System.Windows;

namespace SchoolSchedule.Services
{
    public static class ThemeService
    {
        // Текущая тема — true = тёмная, false = светлая
        public static bool IsDark { get; private set; }

        // Событие — срабатывает когда тема меняется
        public static event Action? ThemeChanged;

        // Главный метод смены темы
        public static void Apply(bool dark)
        {
            IsDark = dark;

            var dict = new ResourceDictionary();

            if (dark)
                dict.Source = new Uri("pack://application:,,,/Themes/DarkTheme.xaml",
                                      UriKind.Absolute);
            else
                dict.Source = new Uri("pack://application:,,,/Themes/LightTheme.xaml",
                                      UriKind.Absolute);

            // Находим старый словарь темы и заменяем его
            var merged = Application.Current.Resources.MergedDictionaries;
            var old = merged.FirstOrDefault(d =>
                d.Source != null &&
                (d.Source.OriginalString.Contains("DarkTheme") ||
                 d.Source.OriginalString.Contains("LightTheme")));

            if (old != null)
                merged.Remove(old);

            merged.Add(dict);

            // Уведомляем всех подписчиков (в т.ч. MainWindow → titlebar)
            ThemeChanged?.Invoke();

            // Сохраняем выбор пользователя
            Properties.Settings.Default.IsDarkTheme = dark;
            Properties.Settings.Default.Save();
        }

        // Вызывается при старте приложения — восстанавливает сохранённую тему
        public static void ApplySaved()
        {
            Apply(Properties.Settings.Default.IsDarkTheme);
        }
    }
}
