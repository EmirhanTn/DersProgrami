using DersProgrami.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DersProgrami.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

    

            modelBuilder.Entity<PendingTeacherRequest>()
                .HasOne(p => p.Faculty)
                .WithMany()
                .HasForeignKey(p => p.FacultyId)
                .OnDelete(DeleteBehavior.NoAction);   // <-- ÖNEMLİ

            modelBuilder.Entity<PendingTeacherRequest>()
                .HasOne(p => p.Department)
                .WithMany()
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.NoAction);   // <-- ÖNEMLİ

            modelBuilder.Entity<Teacher>()
                .HasIndex(t => t.UserId)
                .IsUnique();


         
            modelBuilder.Entity<Lesson>()
                .HasIndex(l => new { l.DepartmentId, l.Code })
                .IsUnique();


            // Schedule - Teacher ilişkisinde Cascade'i kapat
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Teacher)
                .WithMany(t => t.Schedules)
                .HasForeignKey(s => s.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // Schedule - Lesson ilişkisinde Cascade'i kapat
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Lesson)
                .WithMany()
                .HasForeignKey(s => s.LessonId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Schedule>()
                .HasIndex(s => new { s.TeacherId, s.Day, s.Hour })
                .IsUnique();
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TimeSlot> TimeSlots { get; set; }

        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<PendingTeacherRequest> PendingTeacherRequests { get; set; }
        public DbSet<Announcement> Announcements { get; set; } = default!;


        public DbSet<SalaryCoefficient> SalaryCoefficients { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }


    }
}
