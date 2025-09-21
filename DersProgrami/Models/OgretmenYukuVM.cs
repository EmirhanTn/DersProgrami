namespace DersProgrami.Models
{
    public class OgretmenYukuVM
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = "";

        public Dictionary<DayOfWeek, int> HoursByDay { get; set; } =
            new() {
                { DayOfWeek.Monday, 0 }, { DayOfWeek.Tuesday, 0 },
                { DayOfWeek.Wednesday, 0 }, { DayOfWeek.Thursday, 0 },
                { DayOfWeek.Friday, 0 }
            };

        public List<(string Code, string Name, int Hours)> ByLesson { get; set; } = new();

        public int Total => HoursByDay.Values.Sum();
    }
}
