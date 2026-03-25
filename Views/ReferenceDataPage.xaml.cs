using Microsoft.Extensions.DependencyInjection;
using SchoolSchedule.Services;
using SchoolSchedule.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace SchoolSchedule.Views
{
    public partial class ReferenceDataPage : Page
    {
        private readonly ReferenceDataViewModel _vm;
        private readonly AppDataChangedNotifier _notifier;
        private readonly Dictionary<int, UIElement> _tabCache = new Dictionary<int, UIElement>();
        private bool _initialized;

        private static readonly string[] TabNames =
            { "Кабинеты", "Учителя", "Предметы", "Назначения", "Конфликты" };

        public ReferenceDataPage()
        {
            InitializeComponent();
            _vm = App.Services.GetRequiredService<ReferenceDataViewModel>();
            _notifier = App.Services.GetRequiredService<AppDataChangedNotifier>();
            _notifier.DataChanged += OnDataChangedAsync;
            BuildTabButtons();
            Loaded += OnLoaded;
        }

        private void BuildTabButtons()
        {
            TabButtonsPanel.Children.Clear();
            for (int i = 0; i < TabNames.Length; i++)
            {
                int idx = i;
                var btn = new Button
                {
                    Content = TabNames[i],
                    Margin = new Thickness(0, 0, 4, 0),
                    Padding = new Thickness(12, 7, 12, 7),
                    FontSize = 13,
                    Background = new SolidColorBrush(i == 4
                        ? Color.FromRgb(0xD4, 0xA3, 0x00)
                        : Color.FromRgb(0x4A, 0x90, 0xD9)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                btn.Click += (_, __) => { _vm.CurrentTab = idx; ShowTab(idx); };
                TabButtonsPanel.Children.Add(btn);
            }
        }

        private async void OnLoaded(object s, RoutedEventArgs e)
        {
            if (_initialized) return;
            _initialized = true;
            await _vm.InitializeAsync();
            _tabCache.Clear();
            ShowTab(_vm.CurrentTab);
        }

        private async Task OnDataChangedAsync()
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                _initialized = false;
                _tabCache.Clear();
                await _vm.InitializeAsync();
                ShowTab(_vm.CurrentTab);
            });
        }

        private void ShowTab(int tab)
        {
            if (!_tabCache.TryGetValue(tab, out var content))
            {
                content = tab switch
                {
                    0 => BuildRoomsTab(),
                    1 => BuildTeachersTab(),
                    2 => BuildSubjectsTab(),
                    3 => BuildAssignmentsTab(),
                    4 => BuildConflictsTab(),
                    _ => new TextBlock { Text = "Неизвестная вкладка" }
                };
                _tabCache[tab] = content;
            }
            TabContent.Content = content;
        }

        private void InvalidateTab(int tab) => _tabCache.Remove(tab);

        // -- Кабинеты ------------------------------------------------

        private UIElement BuildRoomsTab()
        {
            var sp = new StackPanel { Margin = new Thickness(16) };
            var addBtn = MakeAddButton("Добавить кабинет");
            addBtn.Click += async (_, __) =>
            {
                var dlg = new InputDialog("Добавить кабинет", "Номер кабинета:");
                if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Result)) return;
                await _vm.AddRoomDirectAsync(new Data.Models.Room { Number = dlg.Result });
                InvalidateTab(0); ShowTab(0);
            };
            sp.Children.Add(addBtn);

            var list = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            foreach (var r in _vm.Rooms)
            {
                var rCopy = r;
                list.Children.Add(MakeListRow($"каб. {r.Number}", async () =>
                {
                    if (MessageBox.Show($"Удалить кабинет {rCopy.Number}?", "Удалить",
                        MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                    await _vm.DeleteRoomDirectAsync(rCopy);
                    InvalidateTab(0); ShowTab(0);
                }));
            }
            sp.Children.Add(list);
            return Scroll(sp);
        }

        // -- Учителя -------------------------------------------------

        private UIElement BuildTeachersTab()
        {
            var sp = new StackPanel { Margin = new Thickness(16) };
            var addBtn = MakeAddButton("Добавить учителя");
            addBtn.Click += async (_, __) =>
            {
                var d1 = new InputDialog("Добавить учителя", "Фамилия:");
                if (d1.ShowDialog() != true || string.IsNullOrWhiteSpace(d1.Result)) return;
                var d2 = new InputDialog("Добавить учителя", "Имя:");
                if (d2.ShowDialog() != true || string.IsNullOrWhiteSpace(d2.Result)) return;
                var d3 = new InputDialog("Добавить учителя", "Отчество (необязательно):");
                d3.ShowDialog();
                await _vm.AddTeacherDirectAsync(new Data.Models.Teacher
                {
                    LastName = d1.Result,
                    FirstName = d2.Result,
                    MiddleName = d3.Result ?? string.Empty
                });
                InvalidateTab(1); ShowTab(1);
            };
            sp.Children.Add(addBtn);

            var list = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            foreach (var t in _vm.Teachers)
            {
                var tCopy = t;
                list.Children.Add(MakeListRow(t.FullName, async () =>
                {
                    if (MessageBox.Show($"Удалить {tCopy.FullName}?", "Удалить",
                        MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                    await _vm.DeleteTeacherDirectAsync(tCopy);
                    InvalidateTab(1); ShowTab(1);
                }));
            }
            sp.Children.Add(list);
            return Scroll(sp);
        }

        // -- Предметы -------------------------------------------------

        private UIElement BuildSubjectsTab()
        {
            var sp = new StackPanel { Margin = new Thickness(16) };
            var addBtn = MakeAddButton("Добавить предмет");
            addBtn.Click += async (_, __) =>
            {
                var dlg = new InputDialog("Добавить предмет", "Название предмета:");
                if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Result)) return;
                await _vm.AddSubjectDirectAsync(new Data.Models.Subject { Name = dlg.Result });
                InvalidateTab(2); ShowTab(2);
            };
            sp.Children.Add(addBtn);

            var list = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            foreach (var s in _vm.Subjects)
            {
                var sCopy = s;
                list.Children.Add(MakeListRow(s.Name, async () =>
                {
                    if (MessageBox.Show($"Удалить '{sCopy.Name}'?", "Удалить",
                        MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                    await _vm.DeleteSubjectDirectAsync(sCopy);
                    InvalidateTab(2); ShowTab(2);
                }));
            }
            sp.Children.Add(list);
            return Scroll(sp);
        }

        // -- Назначения -----------------------------------------------

        private UIElement BuildAssignmentsTab()
        {
            var sp = new StackPanel { Margin = new Thickness(16) };
            var addBtn = MakeAddButton("Добавить назначение");
            addBtn.Click += async (_, __) =>
            {
                await _vm.AddAssignmentWpfAsync();
                InvalidateTab(3); ShowTab(3);
            };
            sp.Children.Add(addBtn);
            sp.Children.Add(new TextBlock
            {
                Text = $"Всего назначений: {_vm.Assignments.Count}",
                FontSize = 13,
                Margin = new Thickness(0, 6, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))
            });

            var list = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            foreach (var d in _vm.AssignmentDisplays)
            {
                var dCopy = d;
                var inner = new StackPanel();
                inner.Children.Add(new TextBlock { Text = $"Класс: {d.ClassName}", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)) });
                inner.Children.Add(new TextBlock { Text = $"Предмет: {d.SubjectName}", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)) });
                inner.Children.Add(new TextBlock { Text = $"Учитель: {d.TeacherName}", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)) });
                inner.Children.Add(new TextBlock { Text = $"каб. {d.RoomNumber}", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)) });

                var delBtn = new Button
                {
                    Content = "Удалить",
                    FontSize = 11,
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 6, 0, 0),
                    Background = new SolidColorBrush(Color.FromRgb(0xCC, 0x33, 0x33)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Cursor = Cursors.Hand
                };
                delBtn.Click += async (_, __) =>
                {
                    if (MessageBox.Show("Удалить назначение?", "Удалить",
                        MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                    await _vm.DeleteAssignmentByIdAsync(dCopy.Id);
                    InvalidateTab(3); ShowTab(3);
                };
                inner.Children.Add(delBtn);

                list.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xEF, 0xFF)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xBB, 0xCC, 0xEE)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 9, 12, 9),
                    Margin = new Thickness(0, 0, 0, 6),
                    Child = inner
                });
            }
            sp.Children.Add(list);
            return Scroll(sp);
        }

        // -- Конфликты ------------------------------------------------

        private UIElement BuildConflictsTab()
        {
            var sp = new StackPanel { Margin = new Thickness(16) };
            sp.Children.Add(new TextBlock
            {
                Text = $"Классы с недостающими предметами: {_vm.Conflicts.Count}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xD4, 0xA3, 0x00)),
                Margin = new Thickness(0, 0, 0, 10)
            });

            foreach (var c in _vm.Conflicts)
            {
                var inner = new StackPanel();
                inner.Children.Add(new TextBlock
                {
                    Text = c.ClassName,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xD4, 0xA3, 0x00))
                });
                inner.Children.Add(new TextBlock
                {
                    Text = $"Не назначено предметов: {c.MissingSubjects.Count}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88))
                });
                inner.Children.Add(new TextBlock
                {
                    Text = c.MissingSubjectsText,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xD4, 0xA3, 0x00))
                });

                sp.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF8, 0xE6)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xD4, 0xA3, 0x00)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 9, 12, 9),
                    Margin = new Thickness(0, 0, 0, 6),
                    Child = inner
                });
            }
            return Scroll(sp);
        }

        // -- Вспомогательные методы ------------------------------------

        private static ScrollViewer Scroll(UIElement content) => new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        private static Button MakeAddButton(string label) => new Button
        {
            Content = label,
            Padding = new Thickness(12, 9, 12, 9),
            FontSize = 13,
            Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Left,
            Cursor = Cursors.Hand
        };

        private static Border MakeListRow(string label, Func<Task> onDelete)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tb = new TextBlock
            {
                Text = label,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A))
            };
            Grid.SetColumn(tb, 0);

            var btn = new Button
            {
                Content = "X",
                FontSize = 15,
                Width = 38,
                Height = 38,
                Padding = new Thickness(4),
                Background = new SolidColorBrush(Color.FromRgb(0xCC, 0x33, 0x33)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btn.Click += async (_, __) => await onDelete();
            Grid.SetColumn(btn, 1);

            grid.Children.Add(tb);
            grid.Children.Add(btn);

            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xEF, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xBB, 0xCC, 0xEE)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 7, 12, 7),
                Margin = new Thickness(0, 0, 0, 6),
                Child = grid
            };
        }
    }
}
