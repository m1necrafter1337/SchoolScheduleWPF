using SchoolSchedule.Data;
using SchoolSchedule.Data.Models;

namespace SchoolSchedule.Services;

/// <summary>
/// Сервис бизнес-логики расписания:
/// - построение плоских view для сетки
/// - обнаружение конфликтов (учитель / кабинет)
/// - валидация недельной нагрузки классов
/// </summary>
public class ScheduleService
{
    private readonly AppDatabase _db;

    public ScheduleService(AppDatabase db)
    {
        _db = db;
    }

    // ------------------------------------------------------------
    //  ПОЛУЧЕНИЕ РАСПИСАНИЯ ДЛЯ СЕТКИ
    // ------------------------------------------------------------

    /// <summary>
    /// Возвращает плоские записи для одного дня — уже с именами, флагами конфликтов.
    /// </summary>
    public async Task<List<ScheduleItemView>> GetDayViewAsync(int dayOfWeek)
    {
        var items = await _db.GetScheduleItemsAsync(dayOfWeek);
        var teachers = (await _db.GetTeachersAsync()).ToDictionary(t => t.Id);
        var classes = (await _db.GetClassesAsync()).ToDictionary(c => c.Id);
        var subjects = (await _db.GetSubjectsAsync()).ToDictionary(s => s.Id);
        var rooms = (await _db.GetRoomsAsync()).ToDictionary(r => r.Id);
        var assignments = (await _db.GetAssignmentsAsync()).ToDictionary(a => a.Id);

        var views = new List<ScheduleItemView>();

        foreach (var item in items)
        {
            if (!assignments.TryGetValue(item.AssignmentId, out var asg)) continue;

            var teacher = asg.TeacherId > 0 && teachers.TryGetValue(asg.TeacherId, out var t) ? t : null;
            var cls = classes.TryGetValue(asg.ClassId, out var c) ? c : null;
            var subject = subjects.TryGetValue(asg.SubjectId, out var s) ? s : null;
            var room = asg.RoomId.HasValue && rooms.TryGetValue(asg.RoomId.Value, out var r) ? r : null;

            views.Add(new ScheduleItemView
            {
                ScheduleItemId = item.Id,
                DayOfWeek = item.DayOfWeek,
                LessonNumber = item.LessonNumber,
                ClassId = asg.ClassId,
                ClassName = cls?.DisplayName ?? "?",
                SubjectId = asg.SubjectId,
                SubjectName = subject?.Name ?? "?",
                TeacherId = asg.TeacherId,
                TeacherShortName = teacher?.ShortName ?? "—",
                TeacherFullName = teacher?.FullName ?? "—",
                RoomNumber = room?.Number ?? "—",
                AssignmentId = asg.Id,
            });
        }

        // Обнаружение конфликтов внутри дня
        MarkConflicts(views);

        return views;
    }

    /// <summary>
    /// Возвращает view по фильтру: один день + один класс.
    /// </summary>
    public async Task<List<ScheduleItemView>> GetDayClassViewAsync(int dayOfWeek, int classId)
    {
        var all = await GetDayViewAsync(dayOfWeek);
        return all.Where(v => v.ClassId == classId).ToList();
    }

    /// <summary>
    /// Возвращает всё расписание учителя по всей неделе.
    /// </summary>
    public async Task<List<ScheduleItemView>> GetTeacherWeekViewAsync(int teacherId)
    {
        var result = new List<ScheduleItemView>();
        for (int day = 0; day < 6; day++)
        {
            var dayView = await GetDayViewAsync(day);
            result.AddRange(dayView.Where(v => v.TeacherId == teacherId));
        }
        return result;
    }

    // ------------------------------------------------------------
    //  КОНФЛИКТЫ
    // ------------------------------------------------------------

    private static void MarkConflicts(List<ScheduleItemView> views)
    {
        // Конфликт учителя: один учитель в одно время в разных классах (не группах одного класса)
        var teacherSlots = views
            .GroupBy(v => (v.TeacherId, v.LessonNumber))
            .Where(g => g.Select(v => v.ClassId).Distinct().Count() > 1);

        foreach (var group in teacherSlots)
            foreach (var view in group)
                view.HasTeacherConflict = true;

        // Конфликт кабинета: один кабинет в одно время у разных записей
        var roomSlots = views
            .Where(v => v.RoomNumber != "—")
            .GroupBy(v => (v.RoomNumber, v.LessonNumber))
            .Where(g => g.Select(v => v.ScheduleItemId).Distinct().Count() > 1);

        foreach (var group in roomSlots)
            foreach (var view in group)
                view.HasRoomConflict = true;
    }

    // ------------------------------------------------------------
    //  ВАЛИДАЦИЯ НЕДЕЛЬНОЙ НАГРУЗКИ
    // ------------------------------------------------------------

    /// <summary>
    /// Возвращает словарь: classId > (текущее кол-во уроков в неделю, максимум).
    /// Если текущее > максимум — нагрузка превышена.
    /// </summary>
    public async Task<Dictionary<int, (int Current, int Max, bool Exceeded)>> GetWeekLoadAsync()
    {
        var classes = await _db.GetClassesAsync();
        var result = new Dictionary<int, (int, int, bool)>();

        foreach (var cls in classes)
        {
            var items = await _db.GetScheduleItemsByClassAsync(cls.Id);
            var count = items.Count;
            result[cls.Id] = (count, cls.MaxLessonsPerWeek, count > cls.MaxLessonsPerWeek);
        }

        return result;
    }

    /// <summary>
    /// Проверяет, не превышает ли добавление урока в конкретный класс лимит.
    /// </summary>
    public async Task<bool> WouldExceedWeekLimitAsync(int classId)
    {
        var cls = (await _db.GetClassesAsync()).FirstOrDefault(c => c.Id == classId);
        if (cls == null) return false;
        var items = await _db.GetScheduleItemsByClassAsync(classId);
        return items.Count >= cls.MaxLessonsPerWeek;
    }

    // ------------------------------------------------------------
    //  ДОБАВЛЕНИЕ / УДАЛЕНИЕ УРОКА
    // ------------------------------------------------------------

    public record AddLessonResult(bool Success, string? Warning = null, string? Error = null);

    public async Task<AddLessonResult> AddLessonAsync(ScheduleItem item, int classId)
    {
        string? warning = null;

        // 1. Проверяем лимит недельной нагрузки
        if (await WouldExceedWeekLimitAsync(classId))
        {
            var cls = (await _db.GetClassesAsync()).FirstOrDefault(c => c.Id == classId);
            warning = $"Превышен лимит уроков в неделю для класса {cls?.DisplayName} ({cls?.MaxLessonsPerWeek} ч.)";
        }

        // 2. Проверяем что слот не занят уже этим же классом
        var existing = await _db.GetScheduleItemsByDayAndClassAsync(item.DayOfWeek, classId);
        if (existing.Any(e => e.LessonNumber == item.LessonNumber))
        {
            return new AddLessonResult(false, Error: "В этом слоте уже есть урок для данного класса.");
        }

        System.Diagnostics.Debug.WriteLine($"[SaveScheduleItem] Saving: Day={item.DayOfWeek}, Lesson={item.LessonNumber}, AssignmentId={item.AssignmentId}");
        await _db.SaveScheduleItemAsync(item);
        System.Diagnostics.Debug.WriteLine($"[SaveScheduleItem] Saved successfully");
        
        return new AddLessonResult(true, Warning: warning);
    }

    public Task DeleteLessonAsync(ScheduleItem item) => _db.DeleteScheduleItemAsync(item);

    // ------------------------------------------------------------
    //  ПОИСК
    // ------------------------------------------------------------

    /// <summary>
    /// Поиск по строке: матчит класс, предмет, учителя.
    /// </summary>
    public async Task<List<ScheduleItemView>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var result = new List<ScheduleItemView>();
        for (int day = 0; day < 6; day++)
            result.AddRange(await GetDayViewAsync(day));

        var q = query.Trim().ToLowerInvariant();
        return result.Where(v =>
            v.ClassName.ToLowerInvariant().Contains(q) ||
            v.SubjectName.ToLowerInvariant().Contains(q) ||
            v.TeacherFullName.ToLowerInvariant().Contains(q) ||
            v.TeacherShortName.ToLowerInvariant().Contains(q) ||
            v.RoomNumber.ToLowerInvariant().Contains(q)
        ).ToList();
    }

}
