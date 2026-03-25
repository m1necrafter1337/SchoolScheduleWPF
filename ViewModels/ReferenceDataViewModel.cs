using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SchoolSchedule.Data;
using SchoolSchedule.Data.Models;
using SchoolSchedule.Services;
using SchoolSchedule.Views;

namespace SchoolSchedule.ViewModels
{
    public class ReferenceDataViewModel : BaseViewModel
    {
        private readonly AppDatabase _db;
        private readonly CurriculumService _curriculumService;

        private int _currentTab = 0;
        public int CurrentTab
        {
            get => _currentTab;
            set => SetField(ref _currentTab, value);
        }

        public ObservableCollection<Room> Rooms { get; } = new ObservableCollection<Room>();
        public ObservableCollection<Teacher> Teachers { get; } = new ObservableCollection<Teacher>();
        public ObservableCollection<Subject> Subjects { get; } = new ObservableCollection<Subject>();
        public ObservableCollection<Assignment> Assignments { get; } = new ObservableCollection<Assignment>();
        public ObservableCollection<AssignmentDisplay> AssignmentDisplays { get; } = new ObservableCollection<AssignmentDisplay>();
        public ObservableCollection<ConflictInfoDisplay> Conflicts { get; } = new ObservableCollection<ConflictInfoDisplay>();

        public ReferenceDataViewModel(AppDatabase db, CurriculumService curriculumService)
        {
            _db = db;
            _curriculumService = curriculumService;
        }

        public async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                var rooms = await _db.GetRoomsAsync();
                Rooms.Clear(); foreach (var r in rooms) Rooms.Add(r);

                var teachers = await _db.GetTeachersAsync();
                Teachers.Clear(); foreach (var t in teachers) Teachers.Add(t);

                var subjects = await _db.GetSubjectsAsync();
                Subjects.Clear(); foreach (var s in subjects) Subjects.Add(s);

                var assignments = await _db.GetAssignmentsAsync();
                Assignments.Clear(); foreach (var a in assignments) Assignments.Add(a);

                var displays = await _db.GetAssignmentsDisplayAsync();
                AssignmentDisplays.Clear(); foreach (var d in displays) AssignmentDisplays.Add(d);

                await RefreshConflictsAsync();
            }
            finally { IsBusy = false; }
        }

        private async Task RefreshConflictsAsync()
        {
            var list = await _curriculumService.GetConflictsAsync();
            Conflicts.Clear();
            foreach (var c in list) Conflicts.Add(new ConflictInfoDisplay(c));
        }

        public async Task AddRoomDirectAsync(Room room)
        {
            await _db.SaveRoomAsync(room);
            Rooms.Add(room);
        }

        public async Task DeleteRoomDirectAsync(Room room)
        {
            await _db.DeleteRoomAsync(room);
            Rooms.Remove(room);
        }

        public async Task AddTeacherDirectAsync(Teacher t)
        {
            await _db.SaveTeacherAsync(t);
            Teachers.Add(t);
        }

        public async Task DeleteTeacherDirectAsync(Teacher t)
        {
            await _db.DeleteTeacherAsync(t);
            Teachers.Remove(t);
        }

        public async Task AddSubjectDirectAsync(Subject s)
        {
            await _db.SaveSubjectAsync(s);
            Subjects.Add(s);
        }

        public async Task DeleteSubjectDirectAsync(Subject s)
        {
            await _db.DeleteSubjectAsync(s);
            Subjects.Remove(s);
        }

        public async Task DeleteAssignmentByIdAsync(int id)
        {
            var a = Assignments.FirstOrDefault(x => x.Id == id);
            if (a == null) return;
            await _db.DeleteAssignmentAsync(a);
            Assignments.Remove(a);
            var d = AssignmentDisplays.FirstOrDefault(x => x.Id == id);
            if (d != null) AssignmentDisplays.Remove(d);
            await RefreshConflictsAsync();
        }

        public async Task AddAssignmentWpfAsync()
        {
            var classes = await _db.GetClassesAsync();
            if (classes.Count == 0) { MessageBox.Show("Нет классов в базе."); return; }
            var d1 = new SelectionDialog("Выберите класс", "Класс:", classes.Select(c => c.DisplayName));
            if (d1.ShowDialog() != true) return;
            var cls = classes.First(c => c.DisplayName == d1.SelectedItem);

            var subjects = await _db.GetSubjectsAsync();
            if (subjects.Count == 0) { MessageBox.Show("Нет предметов в базе."); return; }
            var d2 = new SelectionDialog("Выберите предмет", "Предмет:", subjects.Select(s => s.Name));
            if (d2.ShowDialog() != true) return;
            var subj = subjects.First(s => s.Name == d2.SelectedItem);

            var teachers = await _db.GetTeachersAsync();
            if (teachers.Count == 0) { MessageBox.Show("Нет учителей в базе."); return; }
            var d3 = new SelectionDialog("Выберите учителя", "Учитель:", teachers.Select(t => t.FullName));
            if (d3.ShowDialog() != true) return;
            var teacher = teachers.First(t => t.FullName == d3.SelectedItem);

            var rooms = await _db.GetRoomsAsync();
            int? roomId = null;
            if (rooms.Count > 0)
            {
                var roomItems = new[] { "Не выбирать" }.Concat(rooms.Select(r => r.Number));
                var d4 = new SelectionDialog("Выберите кабинет", "Кабинет:", roomItems);
                if (d4.ShowDialog() == true && d4.SelectedItem != "Не выбирать" && d4.SelectedItem != null)
                    roomId = rooms.First(r => r.Number == d4.SelectedItem).Id;
            }

            var asg = new Assignment
            {
                ClassId = cls.Id,
                SubjectId = subj.Id,
                TeacherId = teacher.Id,
                RoomId = roomId
            };
            await _db.SaveAssignmentAsync(asg);
            Assignments.Add(asg);
            await RefreshConflictsAsync();
            MessageBox.Show("Назначение добавлено.", "Успех");
        }
    }

    public class ConflictInfoDisplay
    {
        private readonly CurriculumService.ConflictInfo _info;

        public ConflictInfoDisplay(CurriculumService.ConflictInfo info) => _info = info;

        public int ClassId => _info.ClassId;
        public string ClassName => _info.ClassName;
        public List<string> MissingSubjects => _info.MissingSubjects;
        public string MissingSubjectsText => string.Join(", ", _info.MissingSubjects);
    }
}
