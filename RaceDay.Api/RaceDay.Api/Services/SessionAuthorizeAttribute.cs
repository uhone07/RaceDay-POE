using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Services;

// Usage:
//   [SessionAuthorize]                 -> any logged-in user
//   [SessionAuthorize("Organiser")]    -> Organisers only
//   [SessionAuthorize("Participant")]  -> Participants only
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _requiredRole;

    public SessionAuthorizeAttribute(string? requiredRole = null)
    {
        _requiredRole = requiredRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32("UserId");
        var role = session.GetString("Role");

        // Not logged in
        if (userId == null || role == null)
        {
            context.Result = new UnauthorizedObjectResult(
                new { message = "You must be logged in." });
            return;
        }

        // Logged in, but wrong role
        if (_requiredRole != null && role != _requiredRole)
        {
            context.Result = new ObjectResult(
                new { message = $"This action is for {_requiredRole}s only." })
            {
                StatusCode = 403
            };
        }
    }
}