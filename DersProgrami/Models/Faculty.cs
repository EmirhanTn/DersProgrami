namespace DersProgrami.Models
{
    public class Faculty
    {
        public int FacultyId { get; set; }
        public string Name { get; set; }
        public ICollection<Department> Departments { get; set; }
    }
}
