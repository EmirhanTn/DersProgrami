namespace DersProgrami.Models
{
    public class ScheduleViewModel
    {
        public int Hour { get; set; }
        public Dictionary<DayOfWeek, string> Lessons { get; set; }

        public Dictionary<DayOfWeek, int?> ScheduleIds { get; set; }

        public ScheduleViewModel()
        {
            Lessons = new Dictionary<DayOfWeek, string>();
            ScheduleIds = new Dictionary<DayOfWeek, int?>();
        }

      
        public class AddScheduleDto
        {
            public DayOfWeek Day { get; set; }
            public int Hour { get; set; }          
            public int FacultyId { get; set; }
            public int DepartmentId { get; set; }
            public int LessonId { get; set; }
            public string Classroom { get; set; } = "";
        }

        public class EditScheduleDto
        {
            public int ScheduleId { get; set; }
            public DayOfWeek Day { get; set; }
            public int Hour { get; set; }          
            public int FacultyId { get; set; }
            public int DepartmentId { get; set; }
            public int LessonId { get; set; }
            public string Classroom { get; set; } = "";
        }



    }
}
