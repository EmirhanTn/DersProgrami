namespace DersProgrami.Models
{
    public class Department
    {
        public int DepartmentId { get; set; }
        public string Name { get; set; }
        public int FacultyId { get; set; }
        public Faculty Faculty { get; set; }
        public ICollection<Lesson> Lessons { get; set; }
        public ICollection<Teacher> Teachers { get; set; }
    }
}
