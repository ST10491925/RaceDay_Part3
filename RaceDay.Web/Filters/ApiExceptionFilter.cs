using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Turns an expired or rejected API token into a clean sign-out and redirect,
/// and prevents any other failure from surfacing as a raw stack trace.
/// </summary>
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) => _logger = logger;

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is ApiUnauthorizedException)
        {
            var session = context.HttpContext.RequestServices.GetRequiredService<IUserSession>();
            session.SignOut();
            context.Result = new RedirectToActionResult("Login", "Account", new { expired = true });
            context.ExceptionHandled = true;
            return;
        }

        _logger.LogError(context.Exception, "Unhandled error in {Path}", context.HttpContext.Request.Path);
        context.Result = new RedirectToActionResult("Error", "Home", new { code = 500 });
        context.ExceptionHandled = true;
    }
}
