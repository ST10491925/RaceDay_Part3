using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public abstract class AppController : Controller
{
    protected readonly IRaceDayApi Api;
    protected readonly IUserSession Session;

    protected AppController(IRaceDayApi api, IUserSession session)
    {
        Api = api;
        Session = session;
    }

    protected void Success(string message) => TempData["Success"] = message;
    protected void Error(string message) => TempData["Error"] = message;

    protected IActionResult RedirectToHome() =>
        Session.IsOrganiser ? RedirectToAction("Dashboard", "Organiser") : RedirectToAction("Index", "Home");

    protected IActionResult SafeRedirect(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToHome();
}
