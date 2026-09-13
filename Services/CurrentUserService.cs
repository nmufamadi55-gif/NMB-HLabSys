// File: Services/CurrentUserService.cs
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;

namespace NMB_HLabSys.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _http;
        private readonly ApplicationDbContext _context;

        public CurrentUserService(IHttpContextAccessor http, ApplicationDbContext context)
        {
            _http = http;
            _context = context;
        }

        public int? UserId => _http.HttpContext?.Session.GetInt32("UserID");
        public string? UserRole => _http.HttpContext?.Session.GetString("UserRole");
        public string? Email => _http.HttpContext?.Session.GetString("Email");
        public bool IsAuthenticated => UserId.HasValue;

        public int? DoctorId
        {
            get
            {
                // Real login path (used once login subsystem is wired up)
                if (UserId.HasValue && UserRole == "Doctor")
                {
                    var id = _context.Doctors
                        .Where(d => d.UserID == UserId.Value)
                        .Select(d => (int?)d.DoctorID)
                        .FirstOrDefault();

                    if (id.HasValue) return id.Value;
                }

                // DEV FALLBACK — first doctor in DB. Remove when login works.
#if DEBUG
                return _context.Doctors
                    .OrderBy(d => d.DoctorID)
                    .Select(d => (int?)d.DoctorID)
                    .FirstOrDefault();
#else
                return null;
#endif
            }
        }
    }
}