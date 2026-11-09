using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Protects a controller or action using the session-based sign-in state.
/// Unauthenticated users are redirected to the login page. Signed-in users who
/// hold the wrong role are sent back to their own home area.
/// Roles: "Organiser", "Participant", or empty for any signed-in user.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public string? Role { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.RequestServices.GetRequiredService<IUserSession>();
        var request = context.HttpContext.Request;

        if (!session.IsAuthenticated)
        {
            var returnUrl = request.Method == HttpMethods.Get
                ? request.Path + request.QueryString
                : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (string.IsNullOrEmpty(Role)) return;

        var allowed = Role.Equals("Organiser", StringComparison.OrdinalIgnoreCase)
            ? session.IsOrganiser
            : session.IsParticipant;

        if (!allowed)
        {
            context.Result = session.IsOrganiser
                ? new RedirectToActionResult("Dashboard", "Organiser", null)
                : new RedirectToActionResult("Index", "Home", null);
        }
    }
}
