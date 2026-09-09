using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.Services;
using QuestPDF.Infrastructure;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<ILaboratoryManagerService, LaboratoryManagerService>();
builder.Services.AddScoped<IStockManagementService, StockManagementService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPdfReportService, PdfReportService>();
builder.Services.AddScoped<IPasswordGenerator, PasswordGenerator>();

// QuestPDF License
QuestPDF.Settings.License = LicenseType.Community;

// Add Session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ============================================
// SEED DATABASE - Create Default Manager
// ============================================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Ensure database is created (if using migrations)
    context.Database.Migrate();

    // Check if any Manager exists
    if (!context.Users.Any(u => u.UserRole == "Manager"))
    {
        string rawPassword = "Nmb#Manager2025!";
        string hashedPassword = HashPassword(rawPassword); // Simple SHA256

        var manager = new User
        {
            Email = "manager@nmblab.co.za",
            PasswordHash = hashedPassword,
            UserRole = "Manager",
            IsActive = true,
            IsPasswordChanged = false,
            CreatedDate = DateTime.Now,
            Name = "Manager",
            Surname = "User"
        };

        context.Users.Add(manager);
        context.SaveChanges();

        Console.WriteLine("? Lab Manager created successfully!");
        Console.WriteLine($"?? Email: {manager.Email}");
        Console.WriteLine($"?? Password: {rawPassword}");
        Console.WriteLine("??  Please change your password on first login.");
    }
}

// ============================================
// ENSURE DATABASE IS CREATED
// ============================================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
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

// ============================================
// SIMPLE PASSWORD HASHER
// ============================================
static string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(password);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}