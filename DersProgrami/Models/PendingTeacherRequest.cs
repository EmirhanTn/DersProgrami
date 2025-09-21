namespace DersProgrami.Models
{
    public enum RequestStatus { Pending = 0, Approved = 1, Rejected = 2 }

    public class PendingTeacherRequest
    {
        public int Id { get; set; }
        public string? UserId { get; set; }      
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";

        public int FacultyId { get; set; }
        public Faculty Faculty { get; set; } = null!;

        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
        public string? AdminNote { get; set; }
        public DateTime? DecidedAt { get; set; }
    }
}
