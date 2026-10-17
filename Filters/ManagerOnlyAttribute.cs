using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NMB_HLabSys.Filters
{
    public class ManagerOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var role = context.HttpContext.Session.GetString("UserRole");
            if (!string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }
            base.OnActionExecuting(context);
        }
    }
}