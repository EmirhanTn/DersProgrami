using System.ComponentModel.DataAnnotations;

namespace DersProgrami.Models
{
    public class Announcement
    {
        public int AnnouncementId { get; set; }

        [Required, StringLength(180)]
        public string Title { get; set; } = "";

        [Required]
        public string Content { get; set; } = "";

        public bool IsActive { get; set; } = true;

        [DataType(DataType.DateTime)]
        public DateTime? PublishAt { get; set; }  

        [DataType(DataType.DateTime)]
        public DateTime? ExpireAt { get; set; }   
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedByUserId { get; set; }
    }
}
