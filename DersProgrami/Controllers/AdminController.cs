using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DersProgrami.Data;
using DersProgrami.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<IdentityUser> _userManager;

    public AdminController(ApplicationDbContext ctx, UserManager<IdentityUser> userManager)
    {
        _ctx = ctx;
        _userManager = userManager;
    }

    public IActionResult Index() => View();

    // Fakülte 
    public async Task<IActionResult> Faculties()
    {
        var list = await _ctx.Faculties.OrderBy(x => x.Name).ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult CreateFaculty() => View(new FacultyCreateVM());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFaculty(FacultyCreateVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        _ctx.Faculties.Add(new Faculty { Name = vm.Name?.Trim() });
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Faculties));
    }

    // Bölüm
    public async Task<IActionResult> Departments()
    {
        var list = await _ctx.Departments
            .Include(d => d.Faculty)
            .OrderBy(d => d.Faculty.Name).ThenBy(d => d.Name)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> CreateDepartment()
    {
        ViewBag.Faculties = await _ctx.Faculties.OrderBy(f => f.Name).ToListAsync();
        return View(new DepartmentCreateVM());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDepartment(DepartmentCreateVM vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Faculties = await _ctx.Faculties.OrderBy(f => f.Name).ToListAsync();
            return View(vm);
        }

        _ctx.Departments.Add(new Department
        {
            Name = vm.Name?.Trim(),
            FacultyId = vm.FacultyId
        });
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Departments));
    }

    //Ders
    public async Task<IActionResult> Lessons()
    {
        var list = await _ctx.Lessons
            .Include(l => l.Department).ThenInclude(d => d.Faculty)
            .OrderBy(l => l.Department.Faculty.Name)
            .ThenBy(l => l.Department.Name)
            .ThenBy(l => l.Code)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> CreateLesson()
    {
        ViewBag.Faculties = await _ctx.Faculties.OrderBy(f => f.Name).ToListAsync();
        ViewBag.Departments = new List<Department>();
        return View(new LessonCreateVM());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLesson(LessonCreateVM vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Faculties = await _ctx.Faculties.OrderBy(f => f.Name).ToListAsync();
            ViewBag.Departments = await _ctx.Departments
                .Where(d => d.FacultyId == vm.FacultyId).OrderBy(d => d.Name).ToListAsync();
            return View(vm);
        }

        bool dup = await _ctx.Lessons
            .AnyAsync(l => l.DepartmentId == vm.DepartmentId && l.Code == vm.Code);
        if (dup)
        {
            ModelState.AddModelError(nameof(vm.Code), "Bu bölümde aynı ders kodu zaten var.");
            ViewBag.Faculties = await _ctx.Faculties.OrderBy(f => f.Name).ToListAsync();
            ViewBag.Departments = await _ctx.Departments
                .Where(d => d.FacultyId == vm.FacultyId).OrderBy(d => d.Name).ToListAsync();
            return View(vm);
        }

        _ctx.Lessons.Add(new Lesson
        {
            DepartmentId = vm.DepartmentId,
            Code = vm.Code?.Trim(),
            LessonName = vm.LessonName?.Trim()
        });
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Lessons));
    }

    [HttpGet]
    public async Task<IActionResult> DepartmentsByFaculty(int facultyId)
    {
        var items = await _ctx.Departments
            .Where(d => d.FacultyId == facultyId)
            .OrderBy(d => d.Name)
            .Select(d => new { d.DepartmentId, d.Name })
            .ToListAsync();
        return Json(items);
    }

    // Öğretmenler
    public async Task<IActionResult> Teachers()
    {
        var list = await _ctx.Teachers
            .Include(t => t.Department).ThenInclude(d => d.Faculty)
            .OrderBy(t => t.FullName)
            .ToListAsync();
        return View(list);
    }

    // seçilen öğretmen
    [HttpGet]
    public async Task<IActionResult> TeacherSchedule(int id)
    {
        var ogretmen = await _ctx.Teachers
            .Include(t => t.Department).ThenInclude(d => d.Faculty)
            .FirstOrDefaultAsync(t => t.TeacherId == id);
        if (ogretmen == null) return NotFound();

        ViewBag.Teacher = ogretmen;

        var schedules = await _ctx.Schedules
            .Include(s => s.Lesson).ThenInclude(l => l.Department).ThenInclude(d => d.Faculty)
            .Where(s => s.TeacherId == id)
            .ToListAsync();

        var hours = await _ctx.TimeSlots.OrderBy(t => t.Hour).Select(t => t.Hour).ToListAsync();
        if (hours.Count == 0) hours = Enumerable.Range(9, 10).ToList();

        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        var model = new List<ScheduleViewModel>();
        foreach (var h in hours)
        {
            var row = new ScheduleViewModel { Hour = h };

            foreach (var d in days)
            {
                var sc = schedules.FirstOrDefault(s => s.Day == d && s.Hour == h);
                if (sc == null)
                {
                    row.Lessons[d] = "";
                    row.ScheduleIds[d] = null;
                }
                else
                {
                    var cls = string.IsNullOrWhiteSpace(sc.Classroom) ? "5" : sc.Classroom;
                    row.Lessons[d] =
                        $"<div class='title'>{sc.Lesson.Code} - {sc.Lesson.LessonName}</div>" +
                        $"<div class='sub'>Derslik {cls} · {sc.Lesson.Department.Faculty.Name}</div>";
                    row.ScheduleIds[d] = sc.ScheduleId;
                }
            }

            model.Add(row);
        }

        return View(model);
    }

    // Program 
    [HttpGet]
    public async Task<IActionResult> AddFormAdmin(int teacherId, DayOfWeek day, int hour)
    {
        ViewBag.TeacherId = teacherId;
        ViewBag.Day = day;
        ViewBag.Hour = hour;
        ViewBag.Faculties = await _ctx.Faculties.OrderBy(f => f.Name).ToListAsync();
        ViewBag.Departments = new List<Department>();
        ViewBag.Lessons = new List<Lesson>();
        return PartialView("_AddScheduleFormAdmin",
            new AddScheduleAdminDto { TeacherId = teacherId, Day = day, Hour = hour });
    }

    [HttpGet]
    public async Task<IActionResult> EditFormAdmin(int teacherId, DayOfWeek day, int hour)
    {
        var sc = await _ctx.Schedules
            .Include(s => s.Lesson).ThenInclude(l => l.Department).ThenInclude(d => d.Faculty)
            .FirstOrDefaultAsync(s => s.TeacherId == teacherId && s.Day == day && s.Hour == hour);
        if (sc == null) return NotFound();

        ViewBag.Faculties = await _ctx.Faculties.OrderBy(f => f.Name).ToListAsync();
        ViewBag.Departments = await _ctx.Departments
            .Where(d => d.FacultyId == sc.Lesson.Department.FacultyId).OrderBy(d => d.Name).ToListAsync();
        ViewBag.Lessons = await _ctx.Lessons
            .Where(l => l.DepartmentId == sc.Lesson.DepartmentId).OrderBy(l => l.Code).ToListAsync();

        var vm = new EditScheduleAdminDto
        {
            ScheduleId = sc.ScheduleId,
            TeacherId = teacherId,
            Day = sc.Day,
            Hour = sc.Hour,
            FacultyId = sc.Lesson.Department.FacultyId,
            DepartmentId = sc.Lesson.DepartmentId,
            LessonId = sc.LessonId,
            Classroom = sc.Classroom
        };
        return PartialView("_EditScheduleFormAdmin", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddScheduleAdmin(AddScheduleAdminDto dto)
    {
        var t = await _ctx.Teachers.FindAsync(dto.TeacherId);
        if (t == null) return BadRequest("Öğretmen bulunamadı.");

        bool exists = await _ctx.Schedules.AnyAsync(s =>
            s.TeacherId == dto.TeacherId && s.Day == dto.Day && s.Hour == dto.Hour);
        if (exists) return BadRequest("Bu gün ve saatte kayıt var.");

        if (string.IsNullOrWhiteSpace(dto.Classroom)) dto.Classroom = "5";

        _ctx.Schedules.Add(new Schedule
        {
            TeacherId = dto.TeacherId,
            Day = dto.Day,
            Hour = dto.Hour,
            LessonId = dto.LessonId,
            Classroom = dto.Classroom
        });
        await _ctx.SaveChangesAsync();
        return Ok();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateScheduleAdmin(EditScheduleAdminDto dto)
    {
        var sc = await _ctx.Schedules.FirstOrDefaultAsync(s => s.ScheduleId == dto.ScheduleId);
        if (sc == null || sc.TeacherId != dto.TeacherId) return NotFound();

        bool exists = await _ctx.Schedules.AnyAsync(s =>
            s.TeacherId == dto.TeacherId && s.Day == dto.Day && s.Hour == dto.Hour && s.ScheduleId != dto.ScheduleId);
        if (exists) return BadRequest("Bu gün ve saatte başka kayıt var.");

        sc.Day = dto.Day;
        sc.Hour = dto.Hour;
        sc.LessonId = dto.LessonId;
        sc.Classroom = string.IsNullOrWhiteSpace(dto.Classroom) ? "5" : dto.Classroom;
        await _ctx.SaveChangesAsync();
        return Ok();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteScheduleAdmin(int scheduleId, int teacherId)
    {
        var sc = await _ctx.Schedules.FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);
        if (sc == null || sc.TeacherId != teacherId) return NotFound();

        _ctx.Schedules.Remove(sc);
        await _ctx.SaveChangesAsync();
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> DepByFac(int facultyId)
    {
        var list = await _ctx.Departments
            .Where(d => d.FacultyId == facultyId)
            .OrderBy(d => d.Name)
            .Select(d => new { d.DepartmentId, d.Name })
            .ToListAsync();
        return Json(list);
    }

    [HttpGet]
    public async Task<IActionResult> LessonsByDep(int departmentId)
    {
        var list = await _ctx.Lessons
            .Where(l => l.DepartmentId == departmentId)
            .OrderBy(l => l.Code)
            .Select(l => new { l.LessonId, l.Code, l.LessonName })
            .ToListAsync();
        return Json(list);
    }

    // Saat listesi 
    public async Task<IActionResult> TimeSlots()
    {
        var list = await _ctx.TimeSlots.OrderBy(t => t.Hour).ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult CreateTimeSlot() => View(new TimeSlot());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTimeSlot(TimeSlot vm)
    {
        if (vm.Hour < 0 || vm.Hour > 23)
            ModelState.AddModelError(nameof(vm.Hour), "Saat 0-23 arasında olmalı.");

        bool dup = await _ctx.TimeSlots.AnyAsync(x => x.Hour == vm.Hour);
        if (dup)
            ModelState.AddModelError(nameof(vm.Hour), "Bu saat zaten tanımlı.");

        if (!ModelState.IsValid) return View(vm);

        _ctx.TimeSlots.Add(new TimeSlot { Hour = vm.Hour });
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(TimeSlots));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTimeSlot(int id)
    {
        var slot = await _ctx.TimeSlots.FindAsync(id);
        if (slot == null) return NotFound();

        bool used = await _ctx.Schedules.AnyAsync(s => s.Hour == slot.Hour);
        if (used) return BadRequest("Bu saatte kayıtlı dersler var. Önce onları silin.");

        _ctx.TimeSlots.Remove(slot);
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(TimeSlots));
    }

    //  Öğretmen başvuru 
    [HttpGet]
    public async Task<IActionResult> TeacherRequests()
    {
        var list = await (from t in _ctx.Teachers.Include(x => x.Department).ThenInclude(d => d.Faculty)
                          where !t.IsApproved
                          orderby t.FullName
                          select t).ToListAsync();

        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveTeacher(int id)
    {
        var t = await _ctx.Teachers.FindAsync(id);
        if (t == null) return NotFound();

        t.IsApproved = true;
        await _ctx.SaveChangesAsync();

        TempData["ok"] = "Öğretmen onaylandı.";
        return RedirectToAction(nameof(TeacherRequests));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectTeacher(int id)
    {
        var t = await _ctx.Teachers.FindAsync(id);
        if (t == null) return NotFound();

        var user = await _userManager.FindByIdAsync(t.UserId);

        _ctx.Teachers.Remove(t);
        if (user != null)
            await _userManager.DeleteAsync(user);

        await _ctx.SaveChangesAsync();

        TempData["ok"] = "Başvuru silindi.";
        return RedirectToAction(nameof(TeacherRequests));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTitle(int teacherId, string title)
    {
        var t = await _ctx.Teachers.FindAsync(teacherId);
        if (t == null) return NotFound();

        t.Title = title?.Trim();
        await _ctx.SaveChangesAsync();

        return RedirectToAction(nameof(Teachers));
    }

    [HttpGet]
    public async Task<IActionResult> TeacherWorkload(int id)
    {
        var t = await _ctx.Teachers.FirstOrDefaultAsync(x => x.TeacherId == id);
        if (t == null) return NotFound();

        var schedules = await _ctx.Schedules
            .Include(s => s.Lesson)
            .Where(s => s.TeacherId == id)
            .ToListAsync();

        var vm = new DersProgrami.Models.OgretmenYukuVM
        {
            TeacherId = t.TeacherId,
            TeacherName = t.FullName
        };

        foreach (var g in schedules.GroupBy(s => s.Day))
            vm.HoursByDay[g.Key] = g.Count();

        vm.ByLesson = schedules
            .GroupBy(s => new { s.Lesson.Code, s.Lesson.LessonName })
            .Select(g => (g.Key.Code, g.Key.LessonName, g.Count()))
            .OrderByDescending(x => x.Item3)
            .ToList();

        return View("TeacherWorkload", vm);
    }

    // Maaş katsayı
    public async Task<IActionResult> SalaryCoefficients()
    {
        var list = await _ctx.SalaryCoefficients.OrderBy(c => c.Title).ToListAsync();
        ViewBag.BaseHourly = await _ctx.AppSettings
            .Where(s => s.Key == "Salary.BaseHourly")
            .Select(s => s.Value).FirstOrDefaultAsync() ?? "500";
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCoefficient(int id, decimal coefficient)
    {
        var item = await _ctx.SalaryCoefficients.FindAsync(id);
        if (item == null) return NotFound();
        item.Coefficient = coefficient;
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(SalaryCoefficients));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBaseHourly(decimal baseHourly)
    {
        var setting = await _ctx.AppSettings.FirstOrDefaultAsync(s => s.Key == "Salary.BaseHourly");
        if (setting == null)
        {
            setting = new AppSetting { Key = "Salary.BaseHourly", Value = baseHourly.ToString() };
            _ctx.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = baseHourly.ToString();
        }

        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(SalaryCoefficients));
    }

    // Duyuru
    public async Task<IActionResult> Announcements()
    {
        var list = await _ctx.Announcements
            .OrderByDescending(a => a.PublishAt ?? a.CreatedAt)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult CreateAnnouncement() => View(new Announcement
    {
        PublishAt = DateTime.Now,
        IsActive = true
    });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAnnouncement(Announcement a)
    {
        if (!ModelState.IsValid) return View(a);
        a.CreatedAt = DateTime.UtcNow;
        a.CreatedByUserId = _userManager.GetUserId(User);
        _ctx.Announcements.Add(a);
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Announcements));
    }

    [HttpGet]
    public async Task<IActionResult> EditAnnouncement(int id)
    {
        var a = await _ctx.Announcements.FindAsync(id);
        if (a == null) return NotFound();
        return View(a);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAnnouncement(Announcement a)
    {
        if (!ModelState.IsValid) return View(a);
        _ctx.Update(a);
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Announcements));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAnnouncement(int id)
    {
        var a = await _ctx.Announcements.FindAsync(id);
        if (a != null)
        {
            _ctx.Remove(a);
            await _ctx.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Announcements));
    }
}
