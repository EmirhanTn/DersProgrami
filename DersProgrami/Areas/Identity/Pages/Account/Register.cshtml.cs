using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using DersProgrami.Data;      // ApplicationDbContext
using DersProgrami.Models;    // Teacher, Department, Faculty

namespace DersProgrami.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // Sayfada doldurulan alanlar
        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required, DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required, DataType(DataType.Password), Compare(nameof(Password))]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required, Display(Name = "Ad Soyad")]
            public string FullName { get; set; } = string.Empty;

            [Required, Display(Name = "Fakülte")]
            public int? FacultyId { get; set; }

            [Required, Display(Name = "Bölüm")]
            public int? DepartmentId { get; set; }

            public string? ReturnUrl { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Dropdown kaynakları
        public List<SelectListItem> FacultyOptions { get; set; } = new();
        public List<SelectListItem> DepartmentOptions { get; set; } = new();

        // Sayfa açılışında listeleri doldur
        public async Task OnGetAsync(string? returnUrl = null)
        {
            Input.ReturnUrl = returnUrl;

            FacultyOptions = await _context.Faculties
                .OrderBy(f => f.Name)
                .Select(f => new SelectListItem { Value = f.FacultyId.ToString(), Text = f.Name })
                .ToListAsync();

            if (Input.FacultyId is int fid && fid > 0)
            {
                DepartmentOptions = await _context.Departments
                    .Where(d => d.FacultyId == fid)
                    .OrderBy(d => d.Name)
                    .Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = d.Name })
                    .ToListAsync();
            }
        }

        // Fakülte -> Bölüm: JSON
        // /Identity/Account/Register?handler=Departments&facultyId=3
        public async Task<IActionResult> OnGetDepartmentsAsync(int facultyId)
        {
            var items = await _context.Departments
                .Where(d => d.FacultyId == facultyId)
                .OrderBy(d => d.Name)
                .Select(d => new { d.DepartmentId, d.Name })
                .ToListAsync();

            return new JsonResult(items);
        }

        // Kayıt işlemi
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            Input.ReturnUrl ??= returnUrl;

            if (!ModelState.IsValid)
            {
                // Dropdownlar boş kalmasın
                await OnGetAsync(Input.ReturnUrl);
                return Page();
            }

            // 1) Identity User oluştur
            var user = new IdentityUser { UserName = Input.Email, Email = Input.Email, EmailConfirmed = true };
            var createResult = await _userManager.CreateAsync(user, Input.Password);

            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);

                await OnGetAsync(Input.ReturnUrl);
                return Page();
            }

            // 2) Teacher rolü yoksa oluştur
            if (!await _roleManager.RoleExistsAsync("Teacher"))
                await _roleManager.CreateAsync(new IdentityRole("Teacher"));

            await _userManager.AddToRoleAsync(user, "Teacher");

            // 3) Teacher kaydı
            var teacher = new Teacher
            {
                Email = Input.Email,
                UserId = user.Id,
                FullName = Input.FullName.Trim(),
                DepartmentId = Input.DepartmentId!.Value,
                Title = "Öğretim Görevlisi",
                IsApproved = false // onay süreci istiyorsanız; yoksa kaldırın
            };
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            // 4) İsterseniz hemen oturum açtırabilirsiniz:
            // await _signInManager.SignInAsync(user, isPersistent: false);

            // Onay süreci varsa girişe yönlendirebiliriz
            return RedirectToPage("./Login", new { area = "Identity" });
        }
    }
}
