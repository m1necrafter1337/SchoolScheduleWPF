using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using SchoolSchedule.Data;
using SchoolSchedule.Data.Models;
using SchoolSchedule.Services;
using SchoolSchedule.ViewModels;

namespace SchoolSchedule.Views
{
    public partial class SchedulePage : Page
    {
        private readonly ScheduleViewModel _vm;
        private readonly AppDatabase _db;
        private readonly ScheduleService _svc;
        private readonly AppDataChangedNotifier _notifier;

        private int _selectedClassId;
        private int _selectedLesson;
        private bool _initialized;

        public SchedulePage()
        {
            InitializeComponent();
            _vm = App.Services.GetRequiredService<ScheduleViewModel>();
            _db = App.Services.GetRequiredService<AppDatabase>();
            _svc = App.Services.GetRequiredService<ScheduleService>();
            _notifier = App.Services.GetRequiredService<AppDataChangedNotifier>();
            _notifier.DataChanged += OnDataChangedAsync;
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object s, RoutedEventArgs e)
        {
            if (_initialized) return;
            _initialized = true;
            BuildDayButtons();
            SetBusy(true);
            await _vm.InitializeAsync();
            SetBusy(false);
            BuildGrid();
        }

        private async Task OnDataChangedAsync()
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                SetBusy(true);
                await _vm.InitializeAsync();
                SetBusy(false);
                BuildGrid();
            });
        }

        private void SetBusy(bool busy)
            => BusyIndicator.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;

        // -- Кнопки дней ---------------------------------------------

        private void BuildDayButtons()
        {
            DayButtonsPanel.Children.Clear();
            for (int i = 0; i < _vm.DayButtons.Count; i++)
            {
                int idx = i;
                var btn = new Button
                {
                    Content = _vm.DayButtons[i].Name,
                    Tag = i,
                    Margin = new Thickness(0, 0, 4, 0),
                    Padding = new Thickness(10, 5, 10, 5),
                    FontSize = 13,
                    BorderThickness = new Thickness(0),
                    Foreground = Brushes.White,
                    Cursor = Cursors.Hand
                };
                btn.Click += (_, __) => SelectDay(idx);
                DayButtonsPanel.Children.Add(btn);
            }
            UpdateDayHighlight();
        }

        private async void SelectDay(int idx)
        {
            _vm.SelectedDay = idx;
            UpdateDayHighlight();
            SetBusy(true);
            await _vm.LoadScheduleAsync();
            SetBusy(false);
            BuildGrid();
        }

        private void UpdateDayHighlight()
        {
            TxtDayName.Text = _vm.SelectedDayName;
            foreach (Button b in DayButtonsPanel.Children)
            {
                bool active = (int)b.Tag == _vm.SelectedDay;
                b.Background = new SolidColorBrush(active
                    ? Color.FromRgb(0x2A, 0x52, 0x98)
                    : Color.FromRgb(0x4A, 0x90, 0xD9));
                b.FontWeight = active ? FontWeights.Bold : FontWeights.Normal;
            }
        }

        // -- Сетка расписания -----------------------------------------

        private void BuildGrid()
        {
            SidePanel.Visibility = Visibility.Collapsed;
            SidePanelCol.Width = new GridLength(0);

            ScheduleGrid.Children.Clear();
            ScheduleGrid.RowDefinitions.Clear();
            ScheduleGrid.ColumnDefinitions.Clear();

            var classes = _vm.Classes.ToList();
            var lessons = _vm.LessonNumbers;

            // Колонки: номер урока + по одной на класс
            ScheduleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            foreach (var _ in classes)
                ScheduleGrid.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Строки: заголовки, нагрузка, уроки
            ScheduleGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
            ScheduleGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
            foreach (var _ in lessons)
                ScheduleGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });

            // Заголовки классов
            Add(ScheduleGrid, MakeHeader("№"), 0, 0);
            for (int col = 0; col < classes.Count; col++)
                Add(ScheduleGrid, MakeHeader(classes[col].DisplayName), col + 1, 0);

            // Строка нагрузки
            Add(ScheduleGrid, new Border { Background = HeaderBrush() }, 0, 1);
            for (int col = 0; col < classes.Count; col++)
            {
                var cls = classes[col];
                var exceeded = _vm.IsWeekLoadExceeded(cls.Id);
                Add(ScheduleGrid, new TextBlock
                {
                    Text = _vm.GetWeekLoadText(cls.Id),
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(exceeded
                        ? Color.FromRgb(0xFF, 0x45, 0x00)
                        : Color.FromRgb(0x88, 0x88, 0x88))
                }, col + 1, 1);
            }

            // Ячейки уроков
            for (int row = 0; row < lessons.Count; row++)
            {
                int lesson = lessons[row];
                int gridRow = row + 2;
                Add(ScheduleGrid, MakeLessonNum(lesson), 0, gridRow);
                for (int col = 0; col < classes.Count; col++)
                    Add(ScheduleGrid,
                        MakeCell(_vm.GetCell(classes[col].Id, lesson), classes[col].Id, lesson),
                        col + 1, gridRow);
            }
        }

        private static void Add(Grid g, UIElement el, int col, int row)
        {
            Grid.SetColumn(el, col);
            Grid.SetRow(el, row);
            g.Children.Add(el);
        }

        private static SolidColorBrush HeaderBrush()
            => new SolidColorBrush(Color.FromRgb(0x2A, 0x52, 0x98));

        private static UIElement MakeHeader(string text) => new Border
        {
            Background = HeaderBrush(),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.White
            },
            Padding = new Thickness(2)
        };

        private static UIElement MakeLessonNum(int n) => new Border
        {
            Background = HeaderBrush(),
            Child = new TextBlock
            {
                Text = n.ToString(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            }
        };

        private UIElement MakeCell(CellData cell, int classId, int lessonNumber)
        {
            var borderColor = cell.HasConflict
                ? Color.FromRgb(0xD4, 0xAF, 0x37)
                : Color.FromRgb(0xBB, 0xCC, 0xEE);
            double stroke = cell.HasConflict ? 2.0 : 0.5;

            if (cell.IsEmpty)
            {
                var empty = new Border
                {
                    BorderBrush = new SolidColorBrush(borderColor),
                    BorderThickness = new Thickness(stroke),
                    Background = Brushes.Transparent,
                    Margin = new Thickness(1),
                    Cursor = Cursors.Hand
                };
                empty.MouseLeftButtonUp += (_, __) => OnCellTapped(classId, lessonNumber);
                return empty;
            }

            var stack = new StackPanel { Margin = new Thickness(3, 2, 3, 2) };
            foreach (var item in cell.Items)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = item.SubjectName,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                stack.Children.Add(new TextBlock
                {
                    Text = item.TeacherShortName,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                stack.Children.Add(new TextBlock
                {
                    Text = $"каб. {item.RoomNumber}",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77))
                });
            }

            var cellBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xD8, 0xE6, 0xFF)),
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(stroke),
                Child = stack,
                Cursor = Cursors.Hand,
                Margin = new Thickness(1)
            };
            cellBorder.MouseLeftButtonUp += (_, __) => OnCellTapped(classId, lessonNumber);

            var delBtn = new Button
            {
                Content = "?",
                FontSize = 9,
                Width = 18,
                Height = 18,
                Padding = new Thickness(0),
                Background = new SolidColorBrush(Color.FromRgb(0xCC, 0x33, 0x33)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 2, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Удалить урок"
            };
            var itemId = cell.Items.FirstOrDefault()?.ScheduleItemId ?? 0;
            delBtn.Click += async (_, __) => await OnDeleteAsync(itemId);

            var outer = new Grid { Margin = new Thickness(1) };
            outer.Children.Add(cellBorder);
            outer.Children.Add(delBtn);
            return outer;
        }

        // -- Боковая панель -------------------------------------------

        private async void OnCellTapped(int classId, int lessonNumber)
        {
            _selectedClassId = classId;
            _selectedLesson = lessonNumber;
            var cls = _vm.Classes.FirstOrDefault(c => c.Id == classId);
            PanelTitle.Text = $"Класс {cls?.DisplayName} — Урок {lessonNumber}";
            await LoadAssignmentsPanelAsync(classId, lessonNumber);
            SidePanel.Visibility = Visibility.Visible;
            SidePanelCol.Width = new GridLength(220);
        }

        private async Task LoadAssignmentsPanelAsync(int classId, int lessonNumber)
        {
            AssignmentsList.Children.Clear();
            try
            {
                var existing = await _db.GetScheduleItemsByDayAndClassAsync(_vm.SelectedDay, classId);
                if (existing.Any(i => i.LessonNumber == lessonNumber))
                {
                    AssignmentsList.Children.Add(new TextBlock
                    {
                        Text = "Урок уже заполнен.\nУдалите его крестиком в ячейке.",
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 12, 0, 0)
                    });
                    return;
                }

                var assignments = await _db.GetAssignmentsByClassAsync(classId);
                if (assignments == null || assignments.Count == 0)
                {
                    AssignmentsList.Children.Add(new TextBlock
                    {
                        Text = "Нет назначений для этого класса",
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                        Margin = new Thickness(0, 12, 0, 0)
                    });
                    return;
                }

                var subjects = (await _db.GetSubjectsAsync()).ToDictionary(s => s.Id);
                var teachers = (await _db.GetTeachersAsync()).ToDictionary(t => t.Id);
                var rooms = (await _db.GetRoomsAsync()).ToDictionary(r => r.Id);

                foreach (var asg in assignments)
                {
                    if (!subjects.TryGetValue(asg.SubjectId, out var subject)) continue;
                    if (!teachers.TryGetValue(asg.TeacherId, out var teacher)) continue;
                    rooms.TryGetValue(asg.RoomId ?? 0, out var room);

                    var sp = new StackPanel();
                    sp.Children.Add(new TextBlock
                    {
                        Text = subject.Name,
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A))
                    });
                    sp.Children.Add(new TextBlock
                    {
                        Text = teacher.ShortName,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44))
                    });
                    if (room != null)
                        sp.Children.Add(new TextBlock
                        {
                            Text = $"каб. {room.Number}",
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88))
                        });

                    var card = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFF)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0xBB, 0xCC, 0xEE)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(10, 8, 10, 8),
                        Margin = new Thickness(0, 0, 0, 6),
                        Cursor = Cursors.Hand,
                        Child = sp
                    };
                    var asgCopy = asg;
                    card.MouseLeftButtonUp += async (_, __) => await OnAssignmentSelectedAsync(asgCopy);
                    AssignmentsList.Children.Add(card);
                }
            }
            catch (Exception ex)
            {
                AssignmentsList.Children.Add(new TextBlock
                {
                    Text = $"Ошибка: {ex.Message}",
                    FontSize = 11,
                    Foreground = Brushes.Red,
                    Margin = new Thickness(8)
                });
            }
        }

        private async Task OnAssignmentSelectedAsync(Assignment asg)
        {
            var result = await _svc.AddLessonAsync(new ScheduleItem
            {
                DayOfWeek = _vm.SelectedDay,
                LessonNumber = _selectedLesson,
                AssignmentId = asg.Id
            }, asg.ClassId);

            if (result.Error != null)
            {
                MessageBox.Show(result.Error, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (result.Warning != null)
                MessageBox.Show(result.Warning, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);

            SetBusy(true);
            await _vm.RefreshScheduleAsync();
            SetBusy(false);
            BuildGrid();
            SidePanel.Visibility = Visibility.Collapsed;
            SidePanelCol.Width = new GridLength(0);
        }

        private async Task OnDeleteAsync(int id)
        {
            if (id == 0) return;
            if (MessageBox.Show("Удалить урок?", "Удалить",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            var all = await _db.GetAllScheduleItemsAsync();
            var item = all.FirstOrDefault(i => i.Id == id);
            if (item != null) await _db.DeleteScheduleItemAsync(item);

            SetBusy(true);
            await _vm.InitializeAsync();
            SetBusy(false);
            BuildGrid();
        }

        private void OnClosePanelClicked(object sender, RoutedEventArgs e)
        {
            SidePanel.Visibility = Visibility.Collapsed;
            SidePanelCol.Width = new GridLength(0);
        }
    }
}
