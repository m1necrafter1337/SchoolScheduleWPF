using System.Linq;

namespace SchoolSchedule.Helpers
{
    public static class DayHelper
    {
        public static readonly string[] DayNames =
        {
            "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота"
        };

        public static string GetName(int dayIndex) =>
            dayIndex >= 0 && dayIndex < DayNames.Length ? DayNames[dayIndex] : "?";

        public const int MaxLessonsPerDay = 8;
        public static readonly int[] LessonNumbers = Enumerable.Range(1, MaxLessonsPerDay).ToArray();
    }
}
