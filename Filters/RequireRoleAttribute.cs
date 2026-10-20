using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace NMB_HLabSys.Filters;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;
    public RequireRoleAttribute(params string[] roles) => _roles = roles;
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var role = context.HttpContext.Session.GetString("UserRole");
        if (string.IsNullOrWhiteSpace(role)) { context.Result = new RedirectToActionResult("Login", "Account", null); return; }
        if (!_roles.Contains(role, StringComparer.OrdinalIgnoreCase)) context.Result = new ForbidResult();
    }
}


