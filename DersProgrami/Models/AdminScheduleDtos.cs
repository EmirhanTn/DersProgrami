namespace DersProgrami.Models
{
    public class AddScheduleAdminDto
    {
        public int TeacherId { get; set; }
        public DayOfWeek Day { get; set; }
        public int Hour { get; set; }
        public int FacultyId { get; set; }
        public int DepartmentId { get; set; }
        public int LessonId { get; set; }
        public string Classroom { get; set; } = "";
    }

    public class EditScheduleAdminDto
    {
        public int ScheduleId { get; set; }
        public int TeacherId { get; set; }
        public DayOfWeek Day { get; set; }
        public int Hour { get; set; }
        public int FacultyId { get; set; }
        public int DepartmentId { get; set; }
        public int LessonId { get; set; }
        public string Classroom { get; set; } = "";
    }
}
