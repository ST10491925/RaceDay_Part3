using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : AppController
{
    public AccountController(IRaceDayApi api, IUserSession session) : base(api, session) { }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, bool expired = false)
    {
        if (Session.IsAuthenticated) return RedirectToHome();
        if (expired) ViewBag.Notice = "Your session has ended. Sign in again to continue.";
        return View(new LoginForm { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);

        var result = await Api.LoginAsync(form.Email.Trim(), form.Password);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Sign in failed.");
            form.Password = "";
            return View(form);
        }

        Session.SignIn(result.Data!, fallbackName: form.Email.Trim());

        // Prefer the real name from the profile when the token does not carry one.
        try
        {
            var profile = await Api.GetProfileAsync();
            if (profile.Success && profile.Data!.FullName.Length > 0)
            {
                Session.UpdateDisplayName(profile.Data.FullName);
            }
        }
        catch (ApiUnauthorizedException)
        {
            // Ignore: the token was just issued, so this only happens if the API is misconfigured.
        }

        Success($"Welcome back, {Session.DisplayName}.");
        return SafeRedirect(form.ReturnUrl);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (Session.IsAuthenticated) return RedirectToHome();
        return View(new RegisterForm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterForm form)
    {
        if (!ModelState.IsValid) return View(form);

        var result = await Api.RegisterAsync(form);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
            form.Password = "";
            form.ConfirmPassword = "";
            return View(form);
        }

        Success("Your account is ready. Sign in to start entering events.");
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Session.SignOut();
        Success("You have been signed out.");
        return RedirectToAction("Index", "Home");
    }
}
