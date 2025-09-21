using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DersProgrami.Data;
using DersProgrami.Models;                    
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static DersProgrami.Models.ScheduleViewModel;

namespace DersProgrami.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        private static readonly DayOfWeek[] WeekDays =
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday
        };

        public TeacherController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Teacher?> AktifOgretmen()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _context.Teachers
                .Include(t => t.Department).ThenInclude(d => d.Faculty)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);
        }
        private async Task<List<int>> SaatSlot()
        {
            var hours = await _context.TimeSlots
                .OrderBy(t => t.Hour)
                .Select(t => t.Hour)
                .ToListAsync();

            return hours.Count > 0 ? hours : Enumerable.Range(9, 10).ToList(); 
        }

        private static List<ScheduleViewModel> BosTablo(IEnumerable<int> hours)
        {
            var list = new List<ScheduleViewModel>();
            foreach (var h in hours)
            {
                var row = new ScheduleViewModel { Hour = h };
                foreach (var d in WeekDays) row.Lessons[d] = "";
                list.Add(row);
            }
            return list;
        }

        private static string DayTr(DayOfWeek d) => d switch
        {
            DayOfWeek.Monday => "Pazartesi",
            DayOfWeek.Tuesday => "Salı",
            DayOfWeek.Wednesday => "Çarşamba",
            DayOfWeek.Thursday => "Perşembe",
            DayOfWeek.Friday => "Cuma",
            DayOfWeek.Saturday => "Cumartesi",
            DayOfWeek.Sunday => "Pazar",
            _ => d.ToString()
        };

        private static string SaatAraligi(int h) => $"{h:00}:00 - {h + 1:00}:00";

        // Program 

        public async Task<IActionResult> Schedule()
        {
            var teacher = await AktifOgretmen();
            var hours = await SaatSlot();

            if (teacher == null)
            {
                ViewBag.Message = "Hesabınız için Teacher kaydı bulunamadı. Kayıt oluşturup admin onayı bekleyiniz.";
                return View(BosTablo(hours));
            }

            if (!teacher.IsApproved)
            {
                ViewBag.Message = "Başvurunuz henüz admin tarafından onaylanmamış. Onaylandığında programınızı buradan yöneteceksiniz.";
                return View(BosTablo(hours));
            }

            var schedules = await _context.Schedules
                .Include(s => s.Lesson).ThenInclude(l => l.Department).ThenInclude(d => d.Faculty)
                .Where(s => s.TeacherId == teacher.TeacherId)
                .ToListAsync();

            var model = new List<ScheduleViewModel>();
            foreach (var h in hours)
            {
                var row = new ScheduleViewModel { Hour = h };
                foreach (var day in WeekDays)
                {
                    var sc = schedules.FirstOrDefault(s => s.Day == day && s.Hour == h);
                    if (sc == null)
                    {
                        row.Lessons[day] = "";
                    }
                    else
                    {
                        var classroom = string.IsNullOrWhiteSpace(sc.Classroom) ? "5" : sc.Classroom;
                        row.Lessons[day] =
                            $"{sc.Lesson.Code} - {sc.Lesson.LessonName}" +
                            $"<br/><small class='text-muted'>Derslik {classroom} · {sc.Lesson.Department.Faculty.Name}</small>";
                    }
                }
                model.Add(row);
            }

            return View(model);
        }
        //  Formlar 

        [HttpGet]
        public async Task<IActionResult> AddForm(DayOfWeek day, int hour)
        {
            var teacher = await AktifOgretmen();
            if (teacher == null) return BadRequest("Öğretmen kaydı bulunamadı.");
            if (!teacher.IsApproved) return BadRequest("Hesabınız henüz onaylı değil.");

            ViewBag.Day = day;
            ViewBag.Hour = hour;
            ViewBag.Faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
            ViewBag.Departments = new List<Department>();
            ViewBag.Lessons = new List<Lesson>();

            return PartialView("_AddScheduleForm", new AddScheduleDto { Day = day, Hour = hour });
        }

        [HttpGet]
        public async Task<IActionResult> EditForm(DayOfWeek day, int hour)
        {
            var teacher = await AktifOgretmen();
            if (teacher == null) return BadRequest("Öğretmen kaydı bulunamadı.");
            if (!teacher.IsApproved) return BadRequest("Hesabınız henüz onaylı değil.");

            var sc = await _context.Schedules
                .Include(s => s.Lesson).ThenInclude(l => l.Department).ThenInclude(d => d.Faculty)
                .FirstOrDefaultAsync(s => s.TeacherId == teacher.TeacherId && s.Day == day && s.Hour == hour);

            if (sc == null) return NotFound();

            ViewBag.Faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
            ViewBag.Departments = await _context.Departments
                .Where(d => d.FacultyId == sc.Lesson.Department.FacultyId)
                .OrderBy(d => d.Name)
                .ToListAsync();
            ViewBag.Lessons = await _context.Lessons
                .Where(l => l.DepartmentId == sc.Lesson.DepartmentId)
                .OrderBy(l => l.Code)
                .ToListAsync();

            var vm = new EditScheduleDto
            {
                ScheduleId = sc.ScheduleId,
                Day = sc.Day,
                Hour = sc.Hour,
                FacultyId = sc.Lesson.Department.FacultyId,
                DepartmentId = sc.Lesson.DepartmentId,
                LessonId = sc.LessonId,
                Classroom = sc.Classroom
            };
            return PartialView("_EditScheduleForm", vm);
        }
        //  Yazma İşlemleri 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSchedule(AddScheduleDto dto)
        {
            var teacher = await AktifOgretmen();
            if (teacher == null) return BadRequest("Öğretmen kaydı bulunamadı.");
            if (!teacher.IsApproved) return BadRequest("Hesabınız henüz onaylı değil.");

            var active = await _context.TimeSlots.AnyAsync(t => t.Hour == dto.Hour);
            if (!active) return BadRequest("Bu saat pasifleştirildi.");

            var exists = await _context.Schedules.AnyAsync(s =>
                s.TeacherId == teacher.TeacherId && s.Day == dto.Day && s.Hour == dto.Hour);
            if (exists) return BadRequest("Bu gün ve saatte zaten ders var.");

            if (string.IsNullOrWhiteSpace(dto.Classroom)) dto.Classroom = "5";

            var sc = new Schedule
            {
                TeacherId = teacher.TeacherId,
                LessonId = dto.LessonId,
                Day = dto.Day,
                Hour = dto.Hour,
                Classroom = dto.Classroom
            };

            _context.Schedules.Add(sc);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSchedule(EditScheduleDto dto)
        {
            var teacher = await AktifOgretmen();
            if (teacher == null) return BadRequest("Öğretmen kaydı bulunamadı.");
            if (!teacher.IsApproved) return BadRequest("Hesabınız henüz onaylı değil.");

            var active = await _context.TimeSlots.AnyAsync(t => t.Hour == dto.Hour);
            if (!active) return BadRequest("Bu saat pasifleştirildi.");

            var sc = await _context.Schedules.FirstOrDefaultAsync(s => s.ScheduleId == dto.ScheduleId);
            if (sc == null || sc.TeacherId != teacher.TeacherId) return Unauthorized();

            var exists = await _context.Schedules.AnyAsync(s =>
                s.TeacherId == teacher.TeacherId &&
                s.Day == dto.Day &&
                s.Hour == dto.Hour &&
                s.ScheduleId != dto.ScheduleId);
            if (exists) return BadRequest("Bu gün ve saatte zaten başka dersiniz var.");

            if (string.IsNullOrWhiteSpace(dto.Classroom)) dto.Classroom = "5";

            sc.Day = dto.Day;
            sc.Hour = dto.Hour;
            sc.LessonId = dto.LessonId;
            sc.Classroom = dto.Classroom;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSchedule(int scheduleId)
        {
            var teacher = await AktifOgretmen();
            if (teacher == null) return BadRequest("Öğretmen kaydı bulunamadı.");
            if (!teacher.IsApproved) return BadRequest("Hesabınız henüz onaylı değil.");

            var sc = await _context.Schedules.FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);
            if (sc == null || sc.TeacherId != teacher.TeacherId) return Unauthorized();

            _context.Schedules.Remove(sc);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // Zincir Dropdown 

        [HttpGet]
        public async Task<IActionResult> DepartmentsByFaculty(int facultyId)
        {
            var items = await _context.Departments
                .Where(d => d.FacultyId == facultyId)
                .OrderBy(d => d.Name)
                .Select(d => new { departmentId = d.DepartmentId, name = d.Name })
                .ToListAsync();

            return Json(items);
        }

        [HttpGet]
        public async Task<IActionResult> LessonsByDepartment(int departmentId)
        {
            var items = await _context.Lessons
                .Where(l => l.DepartmentId == departmentId)
                .OrderBy(l => l.Code)
                .Select(l => new { lessonId = l.LessonId, code = l.Code, lessonName = l.LessonName })
                .ToListAsync();

            return Json(items);
        }

        // İş yükü

        [HttpGet]
        public async Task<IActionResult> Workload()
        {
            var teacher = await AktifOgretmen();
            if (teacher == null)
            {
                TempData["Warn"] = "Hesabınız için Teacher kaydı bulunamadı.";
                return RedirectToAction(nameof(Schedule));
            }
            if (!teacher.IsApproved)
            {
                TempData["Warn"] = "Hesabınız henüz admin tarafından onaylı değil.";
                return RedirectToAction(nameof(Schedule));
            }

            var schedules = await _context.Schedules
                .Include(s => s.Lesson)
                .Where(s => s.TeacherId == teacher.TeacherId)
                .ToListAsync();

            var vm = new OgretmenYukuVM
            {
                TeacherId = teacher.TeacherId,
                TeacherName = teacher.FullName
            };

            foreach (var g in schedules.GroupBy(s => s.Day))
                vm.HoursByDay[g.Key] = g.Count();

            vm.ByLesson = schedules
                .GroupBy(s => new { s.Lesson.Code, s.Lesson.LessonName })
                .Select(g => (g.Key.Code, g.Key.LessonName, g.Count()))
                .OrderByDescending(x => x.Item3)
                .ToList();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Salary()
        {
            var teacher = await AktifOgretmen();
            if (teacher == null)
            {
                TempData["Warn"] = "Hesabınız için Teacher kaydı bulunamadı.";
                return RedirectToAction(nameof(Schedule));
            }
            if (!teacher.IsApproved)
            {
                TempData["Warn"] = "Hesabınız henüz admin tarafından onaylı değil.";
                return RedirectToAction(nameof(Schedule));
            }

            var totalHours = await _context.Schedules
                .Where(s => s.TeacherId == teacher.TeacherId)
                .CountAsync();

            var coef = await _context.SalaryCoefficients
                .Where(c => c.Title == teacher.Title)
                .Select(c => c.Coefficient)
                .FirstOrDefaultAsync();
            if (coef == 0) coef = 1.00m;

            var baseHourlyStr = await _context.AppSettings
                .Where(s => s.Key == "Salary.BaseHourly")
                .Select(s => s.Value)
                .FirstOrDefaultAsync();
            var baseHourly = decimal.TryParse(baseHourlyStr, out var x) ? x : 500m;

            var vm = new OgretmenMaasVM
            {
                TeacherId = teacher.TeacherId,
                TeacherName = teacher.FullName,
                Title = teacher.Title,
                Coefficient = coef,
                BaseHourly = baseHourly,
                TotalHours = totalHours
            };

            return View(vm);
        }

        // Excel 

        [HttpGet]
        public async Task<IActionResult> ExportExcel()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var teacher = await _context.Teachers
                .Include(t => t.Department).ThenInclude(d => d.Faculty)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (teacher == null) return BadRequest("Öğretmen kaydı bulunamadı.");

            var schedules = await _context.Schedules
                .Where(s => s.TeacherId == teacher.TeacherId)
                .Include(s => s.Lesson).ThenInclude(l => l.Department).ThenInclude(d => d.Faculty)
                .OrderBy(s => s.Day).ThenBy(s => s.Hour)
                .ToListAsync();

            var totalHours = schedules.Count;

            decimal coef = teacher.Title switch
            {
                "Profesör" => 1.80m,
                "Doçent" => 1.50m,
                "Dr. Öğr. Üyesi" => 1.30m,
                "Öğretim Görevlisi" or null or "" => 1.00m,
                _ => 1.00m
            };
            const decimal BasePerHour = 1000m;
            decimal salary = totalHours * coef * BasePerHour;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Özet");

            int r = 1;

            ws.Cell(r, 1).Value = $"{teacher.Title ?? "Öğretim Görevlisi"} {teacher.FullName}";
            ws.Cell(r, 2).Value = $"{teacher.Department?.Faculty?.Name} / {teacher.Department?.Name}";
            ws.Range(r, 1, r, 6).Style.Font.SetBold();
            r += 2;

            ws.Cell(r, 1).Value = "Ders Programı";
            ws.Range(r, 1, r, 6).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(12)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#e9f5ff"));
            r++;

            ws.Cell(r, 1).Value = "Gün";
            ws.Cell(r, 2).Value = "Saat";
            ws.Cell(r, 3).Value = "Ders Kodu";
            ws.Cell(r, 4).Value = "Ders Adı";
            ws.Cell(r, 5).Value = "Derslik";
            ws.Cell(r, 6).Value = "Fakülte";
            ws.Range(r, 1, r, 6).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f2f2f2"));
            r++;

            string GunToTR(DayOfWeek d) => d switch
            {
                DayOfWeek.Monday => "Pazartesi",
                DayOfWeek.Tuesday => "Salı",
                DayOfWeek.Wednesday => "Çarşamba",
                DayOfWeek.Thursday => "Perşembe",
                DayOfWeek.Friday => "Cuma",
                DayOfWeek.Saturday => "Cumartesi",
                DayOfWeek.Sunday => "Pazar",
                _ => d.ToString()
            };
            string SaatAraligi(int h) => $"{h:00}:00 - {h + 1:00}:00";

            foreach (var s in schedules)
            {
                ws.Cell(r, 1).Value = GunToTR(s.Day);
                ws.Cell(r, 2).Value = SaatAraligi(s.Hour);
                ws.Cell(r, 3).Value = s.Lesson.Code;
                ws.Cell(r, 4).Value = s.Lesson.LessonName;
                ws.Cell(r, 5).Value = string.IsNullOrWhiteSpace(s.Classroom) ? "5" : s.Classroom;
                ws.Cell(r, 6).Value = s.Lesson.Department.Faculty.Name;
                r++;
            }

            if (totalHours > 0)
                ws.Range((r - totalHours - 1), 1, (r - 1), 6).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                              .Border.SetInsideBorder(XLBorderStyleValues.Dotted);
            ws.Columns(1, 6).AdjustToContents();

            r += 2;

            ws.Cell(r, 1).Value = "Ders Yükü (Haftalık)";
            ws.Range(r, 1, r, 3).Merge().Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.FromHtml("#fff8e1"));
            r++;

            ws.Cell(r, 1).Value = "Toplam Saat";
            ws.Cell(r, 2).Value = totalHours;
            ws.Cell(r, 1).Style.Font.SetBold();
            r += 2;

            ws.Cell(r, 1).Value = "Bölüm";
            ws.Cell(r, 2).Value = "Saat";
            ws.Range(r, 1, r, 2).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f2f2f2"));
            r++;

            var perDept = schedules
                .GroupBy(s => s.Lesson.Department.Name)
                .Select(g => new { Dept = g.Key, Hours = g.Count() })
                .OrderByDescending(x => x.Hours)
                .ToList();

            foreach (var x in perDept)
            {
                ws.Cell(r, 1).Value = x.Dept;
                ws.Cell(r, 2).Value = x.Hours;
                r++;
            }
            if (perDept.Count > 0)
                ws.Range((r - perDept.Count - 1), 1, (r - 1), 2).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                                                   .Border.SetInsideBorder(XLBorderStyleValues.Dotted);
            ws.Columns(1, 3).AdjustToContents();

            r += 2;

            ws.Cell(r, 1).Value = "Maaş Özeti";
            ws.Range(r, 1, r, 3).Merge().Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.FromHtml("#e8f5e9"));
            r++;

            ws.Cell(r, 1).Value = "Unvan"; ws.Cell(r, 2).Value = teacher.Title ?? "Öğretim Görevlisi";
            ws.Cell(r + 1, 1).Value = "Katsayı"; ws.Cell(r + 1, 2).Value = coef;
            ws.Cell(r + 2, 1).Value = "Saat Ücreti"; ws.Cell(r + 2, 2).Value = BasePerHour;
            ws.Cell(r + 3, 1).Value = "Toplam Saat"; ws.Cell(r + 3, 2).Value = totalHours;
            ws.Cell(r + 4, 1).Value = "Hesaplanan Maaş";
            ws.Cell(r + 4, 2).Value = salary;

            ws.Range(r, 1, r + 4, 1).Style.Font.SetBold();
            ws.Column(2).Style.NumberFormat.Format = "#,##0.00 ₺";
            ws.Columns(1, 3).AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var bytes = stream.ToArray();

            var fileName = $"DersProgrami_{teacher.FullName}_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

    }
}
