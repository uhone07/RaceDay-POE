using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Filters;

// Put [RequireRole("Organiser")] on a controller to block everyone else.
// Put [RequireRole] with no role to allow any logged-in user.
public class RequireRoleAttribute : ActionFilterAttribute
{
    private readonly string? _role;

    public RequireRoleAttribute(string? role = null)
    {
        _role = role;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = context.HttpContext.Session.GetString("Role");

        if (string.IsNullOrEmpty(role))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (_role != null && role != _role)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}