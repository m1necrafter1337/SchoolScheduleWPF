using SchoolSchedule.Data;
using SchoolSchedule.Data.Models;

namespace SchoolSchedule.Services;

public class CurriculumService
{
    private readonly AppDatabase _db;

    public CurriculumService(AppDatabase db)
    {
        _db = db;
    }

    public record ConflictInfo(
        int ClassId,
        string ClassName,
        List<string> MissingSubjects,
        List<Subject> AllMissingSubjectObjects);

    /// <summary>Проверяет какие предметы должны быть в классе, но их нет</summary>
    public async Task<List<ConflictInfo>> GetConflictsAsync()
    {
        var conflicts = new List<ConflictInfo>();
        var classes = await _db.GetClassesAsync();
        var subjects = (await _db.GetSubjectsAsync()).ToDictionary(s => s.Name);
        var assignments = await _db.GetAssignmentsAsync();

        foreach (var cls in classes)
        {
            if (!CurriculumData.ClassSubjects.TryGetValue(cls.Grade, out var requiredSubjects))
                continue;

            var classAssignments = assignments.Where(a => a.ClassId == cls.Id).ToList();
            var assignedSubjectNames = classAssignments
                .Select(a => a.SubjectId)
                .Distinct()
                .Select(id => subjects.Values.FirstOrDefault(s => s.Id == id)?.Name)
                .Where(n => n != null)
                .ToHashSet();

            var missing = requiredSubjects
                .Where(name => !assignedSubjectNames.Contains(name))
                .ToList();

            if (missing.Count > 0)
            {
                var missingObjects = missing
                    .Where(name => subjects.TryGetValue(name, out _))
                    .Select(name => subjects[name])
                    .ToList();

                conflicts.Add(new ConflictInfo(
                    cls.Id,
                    cls.DisplayName,
                    missing,
                    missingObjects));
            }
        }

        return conflicts.OrderBy(c => c.ClassId).ToList();
    }
}