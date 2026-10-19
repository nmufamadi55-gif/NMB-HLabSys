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
        public string? Email => _http.HttpContext?.Session.GetString("UserEmail");
        public bool IsAuthenticated => UserId.HasValue;

        public int? DoctorId
        {
            get
            {
                if (!UserId.HasValue) return null;
                if (!string.Equals(UserRole, "Doctor", System.StringComparison.OrdinalIgnoreCase))
                    return null;

                var sessionDoctorId = _http.HttpContext?.Session.GetInt32("DoctorID");
                if (sessionDoctorId.HasValue) return sessionDoctorId.Value;

                var id = _context.Doctors
                    .Where(d => d.UserID == UserId.Value)
                    .Select(d => (int?)d.DoctorID)
                    .FirstOrDefault();

                if (id.HasValue)
                    _http.HttpContext?.Session.SetInt32("DoctorID", id.Value);

                return id;
            }
        }
    }
}