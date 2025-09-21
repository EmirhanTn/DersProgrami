using System.ComponentModel.DataAnnotations;

namespace DersProgrami.Models
{
    public class FacultyCreateVM
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = "";
    }

    public class DepartmentCreateVM
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        public int FacultyId { get; set; }
    }

    public class LessonCreateVM
    {
        [Required, StringLength(20)]
        public string Code { get; set; } = "";       

        [Required, StringLength(150)]
        public string LessonName { get; set; } = "";  

        [Required]
        public int FacultyId { get; set; }

        [Required]
        public int DepartmentId { get; set; }
    }
}
