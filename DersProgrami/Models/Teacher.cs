namespace DersProgrami.Models
{

    public class Teacher
    {

        public string UserId { get; set; }
        public int TeacherId { get; set; }
        public string FullName { get; set; }

        public string Email { get; set; } = "";

        public string Title { get; set; } = "Öğretim Görevlisi";

        public bool IsApproved { get; set; } = false;
        public int DepartmentId { get; set; }
        public Department Department { get; set; }
        public ICollection<Schedule> Schedules { get; set; }
    }
}
