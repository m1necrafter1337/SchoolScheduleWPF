namespace SchoolSchedule.Data.Models;

public class AssignmentDisplay
{
    public int Id { get; set; }
    public string ClassName { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public string RoomNumber { get; set; } = "—";
}