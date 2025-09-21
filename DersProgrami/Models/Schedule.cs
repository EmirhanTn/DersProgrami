using System.ComponentModel.DataAnnotations;

namespace DersProgrami.Models
{
    public class Schedule
    {
        public int ScheduleId { get; set; }
        public int TeacherId { get; set; }
        public int LessonId { get; set; }
        public DayOfWeek Day { get; set; }
        public int Hour { get; set; }

        public Teacher Teacher { get; set; }
        public Lesson Lesson { get; set; }
                

        public string Classroom { get; set; }
    }
}
