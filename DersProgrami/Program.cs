using DersProgrami.Data;
using DersProgrami.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

await app.Services.InitializeDatabaseAsync(app.Environment.IsDevelopment());

app.Run();

static class SeedExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, bool isDev)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await ctx.Database.MigrateAsync();

        foreach (var role in new[] { "Admin", "Teacher" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        if (isDev)
        {
            var admin = await userManager.FindByEmailAsync("admin@uni.com");
            if (admin is null)
            {
                admin = new IdentityUser
                {
                    UserName = "admin@uni.com",
                    Email = "admin@uni.com",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        // Saat slotlarını ilk kurulumda 09-18 olarak aç (istersen devre dışı bırak)
        if (!await ctx.TimeSlots.AnyAsync())
        {
            for (int h = 9; h <= 18; h++)
                ctx.TimeSlots.Add(new TimeSlot { Hour = h });
        }

        // Maaş hesaplaması için varsayılan saat ücreti
        if (!await ctx.AppSettings.AnyAsync(s => s.Key == "Salary.BaseHourly"))
        {
            ctx.AppSettings.Add(new AppSetting { Key = "Salary.BaseHourly", Value = "500" });
        }

        // Unvan katsayıları yoksa ekle
        if (!await ctx.SalaryCoefficients.AnyAsync())
        {
            ctx.SalaryCoefficients.AddRange(
                new SalaryCoefficient { Title = "Öğretim Görevlisi", Coefficient = 1.00m },
                new SalaryCoefficient { Title = "Doçent", Coefficient = 1.30m },
                new SalaryCoefficient { Title = "Profesör", Coefficient = 1.60m }
            );
        }

        await ctx.SaveChangesAsync();
    }
}
