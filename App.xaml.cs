using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SchoolSchedule.Data;
using SchoolSchedule.Services;
using SchoolSchedule.ViewModels;

namespace SchoolSchedule
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SchoolSchedule", "dbSchool.db");

            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            if (!File.Exists(dbPath))
            {
                var resUri = new Uri("pack://application:,,,/Assets/dbSchool.db");
                var sri = GetResourceStream(resUri);
                if (sri != null)
                {
                    using var src = sri.Stream;
                    using var dst = File.Create(dbPath);
                    src.CopyTo(dst);
                }
            }

            AppDatabase.InitializeAsync(dbPath).GetAwaiter().GetResult();

            var sc = new ServiceCollection();
            sc.AddSingleton(AppDatabase.Instance);
            sc.AddSingleton<ScheduleService>();
            sc.AddSingleton<CurriculumService>();
            sc.AddSingleton<AppDataChangedNotifier>();
            sc.AddSingleton<ScheduleViewModel>();
            sc.AddTransient<ReferenceDataViewModel>();
            Services = sc.BuildServiceProvider();

            
            var savedTheme = Properties.Settings.Default.AppTheme;
            if (string.IsNullOrEmpty(savedTheme))
                savedTheme = "Light";

            var dict = new ResourceDictionary();
            dict.Source = savedTheme == "Dark"
                ? new Uri("pack://application:,,,/Themes/DarkTheme.xaml")
                : new Uri("pack://application:,,,/Themes/LightTheme.xaml");

            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(dict);

            var styles = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Styles/Styles.xaml")
            };
            Resources.MergedDictionaries.Add(styles);

        }
    }
}
