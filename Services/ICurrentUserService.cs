// File: Services/ICurrentUserService.cs
namespace NMB_HLabSys.Services
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string? UserRole { get; }
        string? Email { get; }
        int? DoctorId { get; }
        bool IsAuthenticated { get; }
    }
}