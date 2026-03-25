using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using SchoolSchedule.Data;
using SchoolSchedule.Data.Models;
using SchoolSchedule.Helpers;
using SchoolSchedule.Services;

namespace SchoolSchedule.ViewModels
{
    public class ScheduleViewModel : BaseViewModel
    {
        private readonly ScheduleService _scheduleService;
        private readonly AppDatabase _db;

        private int _selectedDay = 0;
        public int SelectedDay
        {
            get => _selectedDay;
            set { if (SetField(ref _selectedDay, value)) OnPropertyChanged(nameof(SelectedDayName)); }
        }

        public string SelectedDayName => DayHelper.GetName(_selectedDay);

        public ObservableCollection<SchoolClass> Classes { get; } = new ObservableCollection<SchoolClass>();
        public List<int> LessonNumbers { get; } = DayHelper.LessonNumbers.ToList();

        public List<DayButton> DayButtons { get; }

        private Dictionary<(int, int), List<ScheduleItemView>> _scheduleData
            = new Dictionary<(int, int), List<ScheduleItemView>>();

        private Dictionary<int, (int Current, int Max, bool Exceeded)> _weekLoad
            = new Dictionary<int, (int, int, bool)>();

        public ScheduleViewModel(ScheduleService scheduleService, AppDatabase db)
        {
            _scheduleService = scheduleService;
            _db = db;
            DayButtons = Enumerable.Range(0, 6)
                .Select(i => new DayButton { Index = i, Name = DayHelper.DayNames[i] })
                .ToList();
        }

        public async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                var classes = await _db.GetClassesAsync();
                Classes.Clear();
                foreach (var c in classes) Classes.Add(c);
                _weekLoad = await _scheduleService.GetWeekLoadAsync();
                await LoadScheduleAsync();
            }
            finally { IsBusy = false; }
        }

        public async Task LoadScheduleAsync()
        {
            IsBusy = true;
            try
            {
                var items = await _scheduleService.GetDayViewAsync(_selectedDay);
                _scheduleData = items
                    .GroupBy(i => (i.ClassId, i.LessonNumber))
                    .ToDictionary(g => g.Key, g => g.ToList());
            }
            finally { IsBusy = false; }
        }

        public async Task RefreshScheduleAsync()
        {
            _weekLoad = await _scheduleService.GetWeekLoadAsync();
            await LoadScheduleAsync();
        }

        public CellData GetCell(int classId, int lessonNumber)
        {
            _scheduleData.TryGetValue((classId, lessonNumber), out var items);
            _weekLoad.TryGetValue(classId, out var load);
            return new CellData
            {
                ClassId = classId,
                LessonNumber = lessonNumber,
                Items = items ?? new List<ScheduleItemView>(),
                WeekLimitExceeded = load.Exceeded
            };
        }

        public string GetWeekLoadText(int classId)
            => _weekLoad.TryGetValue(classId, out var load)
               ? $"{load.Current}/{load.Max}" : "";

        public bool IsWeekLoadExceeded(int classId)
            => _weekLoad.TryGetValue(classId, out var load) && load.Exceeded;
    }

    public class DayButton
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CellData
    {
        public int ClassId { get; set; }
        public int LessonNumber { get; set; }
        public List<ScheduleItemView> Items { get; set; } = new List<ScheduleItemView>();
        public bool WeekLimitExceeded { get; set; }
        public bool HasConflict => Items.Any(i => i.HasTeacherConflict || i.HasRoomConflict);
        public bool IsEmpty => Items.Count == 0;
    }
}
