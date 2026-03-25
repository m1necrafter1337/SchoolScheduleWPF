using SQLite;

namespace SchoolSchedule.Data.Models;

[Table("Teachers")]
public class Teacher
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [NotNull, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string MiddleName { get; set; } = string.Empty;

    [Ignore]
    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();

    public int? DefaultRoomId { get; set; }

    public int? DefaultRoomId2 { get; set; }

    [Ignore]
    public string ShortName
    {
        get
        {
            var fi = string.IsNullOrEmpty(FirstName) ? "" : $"{FirstName[0]}.";
            var mi = string.IsNullOrEmpty(MiddleName) ? "" : $"{MiddleName[0]}.";
            return $"{LastName} {fi}{mi}".Trim();
        }
    }
}
