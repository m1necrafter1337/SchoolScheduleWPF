using SQLite;

namespace SchoolSchedule.Data.Models;

[Table("Classes")]
public class SchoolClass
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public int Grade { get; set; }

    [NotNull]
    public int MaxLessonsPerWeek { get; set; }

    [Ignore]
    public string DisplayName => $"{Grade}";
}

[Table("Subjects")]
public class Subject
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

[Table("Rooms")]
public class Room
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, MaxLength(50)]
    public string Number { get; set; } = string.Empty;
}

[Table("Assignments")]
public class Assignment
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, Indexed]
    public int ClassId { get; set; }

    [NotNull, Indexed]
    public int SubjectId { get; set; }

    [NotNull, Indexed]
    public int TeacherId { get; set; }

    public int? RoomId { get; set; }
}

[Table("ScheduleItems")]
public class ScheduleItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, Indexed]
    public int DayOfWeek { get; set; }

    [NotNull]
    public int LessonNumber { get; set; }

    [NotNull, Indexed]
    public int AssignmentId { get; set; }
}

public class ScheduleItemView
{
    public int ScheduleItemId { get; set; }
    public int DayOfWeek { get; set; }
    public int LessonNumber { get; set; }

    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;

    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;

    public int TeacherId { get; set; }
    public string TeacherShortName { get; set; } = string.Empty;
    public string TeacherFullName { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;

    public int AssignmentId { get; set; }

    public bool HasTeacherConflict { get; set; }
    public bool HasRoomConflict { get; set; }
}
