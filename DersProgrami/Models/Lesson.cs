namespace DersProgrami.Models
{
    public class Lesson
    {
        public int LessonId { get; set; }
        public string Code { get; set; } = "";
        public string LessonName { get; set; }
        public int DepartmentId { get; set; }
        public Department Department { get; set; }
    }
}
