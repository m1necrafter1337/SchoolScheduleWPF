using SQLite;
using SchoolSchedule.Data.Models;

namespace SchoolSchedule.Data;

public class AppDatabase
{
    private static AppDatabase? _instance;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private SQLiteAsyncConnection _db;

    public static AppDatabase Instance => _instance ?? throw new InvalidOperationException("AppDatabase not initialized. Call InitializeAsync() first.");

    private AppDatabase(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    public static async Task InitializeAsync(string dbPath)
    {
        await _semaphore.WaitAsync();
        try
        {
            // убрать: if (_instance != null) return;
            var db = new AppDatabase(dbPath);
            _instance = db;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // ── Публичные методы доступа к данным ──────────────────────────────

    public SQLiteAsyncConnection Connection => _db;

    public Task<List<Teacher>> GetTeachersAsync() =>
        _db.Table<Teacher>().OrderBy(t => t.LastName).ToListAsync();

    public Task<List<SchoolClass>> GetClassesAsync() =>
        _db.Table<SchoolClass>().OrderBy(c => c.Grade).ToListAsync();

    public Task<List<Subject>> GetSubjectsAsync() =>
        _db.Table<Subject>().OrderBy(s => s.Name).ToListAsync();

    public Task<List<Room>> GetRoomsAsync() =>
        _db.Table<Room>().ToListAsync();

    public Task<List<Assignment>> GetAssignmentsAsync() =>
        _db.Table<Assignment>().ToListAsync();

    public Task<List<Assignment>> GetAssignmentsByClassAsync(int classId) =>
        _db.Table<Assignment>().Where(a => a.ClassId == classId).ToListAsync();

    public Task<List<ScheduleItem>> GetScheduleItemsAsync(int dayOfWeek) =>
        _db.Table<ScheduleItem>().Where(s => s.DayOfWeek == dayOfWeek).ToListAsync();

    public Task<List<ScheduleItem>> GetAllScheduleItemsAsync() =>
        _db.Table<ScheduleItem>().ToListAsync();

    public async Task<List<ScheduleItem>> GetScheduleItemsByClassAsync(int classId)
    {
        var assignmentIds = (await _db.Table<Assignment>()
            .Where(a => a.ClassId == classId).ToListAsync())
            .Select(a => a.Id).ToHashSet();

        return (await _db.Table<ScheduleItem>().ToListAsync())
            .Where(s => assignmentIds.Contains(s.AssignmentId)).ToList();
    }

    public async Task<List<ScheduleItem>> GetScheduleItemsByDayAndClassAsync(int dayOfWeek, int classId)
    {
        var assignments = await GetAssignmentsAsync();
        if (assignments == null) return new List<ScheduleItem>();

        var assignmentIds = assignments
            .Where(a => a.ClassId == classId)
            .Select(a => a.Id)
            .ToHashSet();

        var allItems = await GetAllScheduleItemsAsync();
        if (allItems == null) return new List<ScheduleItem>();

        return allItems
            .Where(s => s.DayOfWeek == dayOfWeek && assignmentIds.Contains(s.AssignmentId))
            .ToList();
    }

    // CRUD Teachers
    public Task<int> SaveTeacherAsync(Teacher teacher) =>
        teacher.Id == 0 ? _db.InsertAsync(teacher) : _db.UpdateAsync(teacher);
    public Task<int> DeleteTeacherAsync(Teacher teacher) => _db.DeleteAsync(teacher);

    // CRUD Classes
    public Task<int> SaveClassAsync(SchoolClass cls) =>
        cls.Id == 0 ? _db.InsertAsync(cls) : _db.UpdateAsync(cls);
    public Task<int> DeleteClassAsync(SchoolClass cls) => _db.DeleteAsync(cls);

    // CRUD Subjects
    public Task<int> SaveSubjectAsync(Subject subject) =>
        subject.Id == 0 ? _db.InsertAsync(subject) : _db.UpdateAsync(subject);
    public Task<int> DeleteSubjectAsync(Subject subject) => _db.DeleteAsync(subject);

    // CRUD Rooms
    public Task<int> SaveRoomAsync(Room room) =>
        room.Id == 0 ? _db.InsertAsync(room) : _db.UpdateAsync(room);
    
    // CRUD Assignments
    public Task<int> SaveAssignmentAsync(Assignment assignment) =>
        assignment.Id == 0 ? _db.InsertAsync(assignment) : _db.UpdateAsync(assignment);


    // CRUD ScheduleItems
    public async Task<int> SaveScheduleItemAsync(ScheduleItem item)
    {
        var result = item.Id == 0 
            ? await _db.InsertAsync(item) 
            : await _db.UpdateAsync(item);
        
        // Принудительно флешим данные на диск
        await _db.ExecuteAsync("PRAGMA synchronous = NORMAL");
        
        return result;
    }
    public Task<int> DeleteScheduleItemAsync(ScheduleItem item) => _db.DeleteAsync(item);
    public async Task<List<AssignmentDisplay>> GetAssignmentsDisplayAsync()
    {
        var assignments = await GetAssignmentsAsync();
        var classes = (await GetClassesAsync()).ToDictionary(c => c.Id);
        var subjects = (await GetSubjectsAsync()).ToDictionary(s => s.Id);
        var teachers = (await GetTeachersAsync()).ToDictionary(t => t.Id);
        var rooms = (await GetRoomsAsync()).ToDictionary(r => r.Id);

        return assignments.Select(a => new AssignmentDisplay
        {
            Id = a.Id,
            ClassName = classes.TryGetValue(a.ClassId, out var cls) ? cls.DisplayName : "?",
            SubjectName = subjects.TryGetValue(a.SubjectId, out var subj) ? subj.Name : "?",
            TeacherName = teachers.TryGetValue(a.TeacherId, out var teacher) ? teacher.FullName : "?",
            RoomNumber = a.RoomId.HasValue && rooms.TryGetValue(a.RoomId.Value, out var room) ? room.Number : "—"
        }).ToList();
    }
    public Task<int> DeleteRoomAsync(Room room) => _db.DeleteAsync(room);
    public Task<int> DeleteAssignmentAsync(Assignment assignment) => _db.DeleteAsync(assignment);
    public async Task CloseAsync()
    {
        await _db.CloseAsync();
    }

    public Task ReopenAsync(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        return Task.CompletedTask;
    }
}
