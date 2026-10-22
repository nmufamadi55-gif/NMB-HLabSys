using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ─── ONE registration for MVC + global filters ───
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<NMB_HLabSys.Filters.NoCacheAttribute>();
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── Services ───
builder.Services.AddScoped<ILaboratoryManagerService, LaboratoryManagerService>();
builder.Services.AddScoped<IStockManagementService, StockManagementService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPdfReportService, PdfReportService>();
builder.Services.AddScoped<IPasswordGenerator, PasswordGenerator>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IBarcodeService, BarcodeService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITechnicianService, TechnicianService>();

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        // ═══════════════════════════════════════════════════════════
        // STARTUP: migrate legacy SHA256 hashes → PBKDF2, then seed
        // ═══════════════════════════════════════════════════════════
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            // ─── Migrate legacy hashes ───
            // SHA256 output (base64) is 44 chars. PBKDF2 is 68 chars.
            // Any user still on the old format gets silently upgraded.
            var knownUsers = new Dictionary<string, string>
            {
                ["manager@nmblab.co.za"] = "Nmb#Manager2025!",
                ["doctor@nmblab.co.za"] = "Nmb#Doctor2025!"
            };

            foreach (var (email, plain) in knownUsers)
            {
                var user = context.Users.FirstOrDefault(u => u.Email == email);
                if (user == null) continue;

                if (user.PasswordHash.Length == 44)   // ← legacy SHA256
                {
                    user.PasswordHash = hasher.HashPassword(plain);
                    Console.WriteLine($"🔁 Migrated {email} → PBKDF2");
                }
            }
            context.SaveChanges();

            // ─── Seed manager if missing ───
            if (!context.Users.Any(u => u.UserRole == "Manager"))
            {
                context.Users.Add(new User
                {
                    Name = "Lab",
                    Surname = "Manager",
                    Email = "manager@nmblab.co.za",
                    ContactNumber = "0820000000",
                    PasswordHash = hasher.HashPassword("Nmb#Manager2025!"),
                    UserRole = "Manager",
                    IsActive = true,
                    IsPasswordChanged = true,
                    CreatedDate = DateTime.Now
                });
                context.SaveChanges();
                Console.WriteLine("✅ Manager seeded: manager@nmblab.co.za / Nmb#Manager2025!");
            }

            // ─── Seed doctor if missing ───
            if (!context.Doctors.Any())
            {
                var doctorUser = new User
                {
                    Name = "Sarah",
                    Surname = "Johnson",
                    Email = "doctor@nmblab.co.za",
                    ContactNumber = "0825555678",
                    PasswordHash = hasher.HashPassword("Nmb#Doctor2025!"),
                    UserRole = "Doctor",
                    IsActive = true,
                    IsPasswordChanged = false,
                    CreatedDate = DateTime.Now
                };
                context.Users.Add(doctorUser);
                context.SaveChanges();

                context.Doctors.Add(new Doctor
                {
                    UserID = doctorUser.UserID,
                    HPCSANumber = "HPCSA001",
                    PracticeName = "Johnson Medical Practice",
                    PracticeAddress = "123 Medical Street, Gqeberha, 6001",
                    Specialization = "Haematology",
                    IsVerified = true,
                    RegisteredDate = DateTime.Now
                });
                context.SaveChanges();
                Console.WriteLine("✅ Doctor seeded: doctor@nmblab.co.za / Nmb#Doctor2025!");
            }
    }
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
