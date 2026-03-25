using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using SchoolSchedule.Data;
using SchoolSchedule.Services;

namespace SchoolSchedule.Views
{
    public partial class SettingsPage : Page
    {
        private bool _loading = true;
        private readonly Action<string> _applyTheme;
        private readonly AppDataChangedNotifier _notifier;

        private static string DbPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SchoolSchedule", "dbSchool.db");

        public SettingsPage(Action<string> applyTheme)
        {
            InitializeComponent();
            _applyTheme = applyTheme;
            _notifier = App.Services.GetRequiredService<AppDataChangedNotifier>();

            // Восстанавливаем сохранённую тему
            var saved = Properties.Settings.Default.AppTheme;
            RadioLight.IsChecked = saved == "Light" || string.IsNullOrEmpty(saved);
            RadioDark.IsChecked = saved == "Dark";
            RadioSystem.IsChecked = saved == "System";
            _loading = false;
        }

        private void OnThemeChanged(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            string theme = RadioDark.IsChecked == true ? "Dark"
                         : RadioSystem.IsChecked == true ? "System"
                         : "Light";
            Properties.Settings.Default.AppTheme = theme;
            Properties.Settings.Default.Save();
            _applyTheme(theme);
        }

        private void OnExportClicked(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(DbPath))
            {
                MessageBox.Show("Файл БД не найден.", "Ошибка");
                return;
            }
            var dlg = new SaveFileDialog
            {
                FileName = $"dbSchool_export_{DateTime.Now:yyyyMMdd_HHmmss}.db",
                DefaultExt = ".db",
                Filter = "SQLite DB|*.db"
            };
            if (dlg.ShowDialog() != true) return;
            File.Copy(DbPath, dlg.FileName, overwrite: true);
            MessageBox.Show("БД успешно сохранена.", "Готово");
        }

        private async void OnImportClicked(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Текущие данные будут заменены. Продолжить?",
                "Импорт БД", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes) return;

            var dlg = new OpenFileDialog
            {
                Filter = "SQLite DB|*.db",
                Title = "Выберите файл БД"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                await AppDatabase.Instance.CloseAsync();
                File.Copy(dlg.FileName, DbPath, overwrite: true);
                await AppDatabase.Instance.ReopenAsync(DbPath);
                await _notifier.NotifyDataChangedAsync();
                MessageBox.Show("БД успешно импортирована.", "Готово");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка импорта: {ex.Message}", "Ошибка");
            }
        }

        private async void OnResetClicked(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Всё расписание будет удалено. Продолжить?",
                "Сброс БД", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes) return;

            try
            {
                await AppDatabase.Instance.CloseAsync();
                foreach (var f in new[] { DbPath, DbPath + "-wal", DbPath + "-shm" })
                    if (File.Exists(f)) File.Delete(f);

                var resUri = new Uri("pack://application:,,,/Assets/dbSchool.db");
                var sri = Application.GetResourceStream(resUri);
                if (sri != null)
                {
                    using var src = sri.Stream;
                    using var dst = File.Create(DbPath);
                    await src.CopyToAsync(dst);
                }
                await AppDatabase.Instance.ReopenAsync(DbPath);
                await _notifier.NotifyDataChangedAsync();
                MessageBox.Show("БД сброшена успешно.", "Готово");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сброса: {ex.Message}", "Ошибка");
            }
        }
    }
}
