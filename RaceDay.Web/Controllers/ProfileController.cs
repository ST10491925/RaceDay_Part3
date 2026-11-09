using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize]
public class ProfileController : AppController
{
    public ProfileController(IRaceDayApi api, IUserSession session) : base(api, session) { }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetProfileAsync();
        if (!result.Success)
        {
            Error(result.Error ?? "Your profile could not be loaded.");
            return RedirectToHome();
        }

        var p = result.Data!;
        return View(new ProfilePageViewModel
        {
            Form = new ProfileForm
            {
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                Phone = p.Phone,
                ProfileImageUrl = p.ProfileImageUrl
            }
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([Bind(Prefix = "Form")] ProfileForm form)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), new ProfilePageViewModel { Form = form });
        }

        var result = await Api.UpdateProfileAsync(form);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Your profile could not be saved.");
            return View(nameof(Index), new ProfilePageViewModel { Form = form });
        }

        Session.UpdateDisplayName($"{form.FirstName} {form.LastName}".Trim());
        Success("Your details have been saved.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Picture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            Error("Choose an image to upload.");
            return RedirectToAction(nameof(Index));
        }

        var problem = Format.ValidateImage(picture);
        if (problem is not null)
        {
            Error(problem);
            return RedirectToAction(nameof(Index));
        }

        var result = await Api.UploadProfilePictureAsync(picture);
        if (result.Success) Success("Your profile picture has been updated.");
        else Error(result.Error ?? "The picture could not be uploaded.");
        return RedirectToAction(nameof(Index));
    }
}
