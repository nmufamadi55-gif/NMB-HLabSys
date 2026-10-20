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

    public class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var headers = context.HttpContext.Response.Headers;

            headers["Cache-Control"] = "no-cache, no-store, must-revalidate, private";
            headers["Pragma"] = "no-cache";
            headers["Expires"] = "0";

            base.OnResultExecuting(context);
        }
    }
    public class RequireSessionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var userId = session.GetInt32("UserID");

            if (!userId.HasValue)
            {
                // Preserve the URL they tried to reach so we can redirect after login
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult(
                    "Login", "Account", new { returnUrl = returnUrl.ToString() });
                return;
            }

            base.OnActionExecuting(context);
        }
    }

}