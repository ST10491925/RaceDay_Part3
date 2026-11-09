using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Role = "Organiser")]
public class ManageEventsController : AppController
{
    public ManageEventsController(IRaceDayApi api, IUserSession session) : base(api, session) { }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetEventsAsync();
        if (!result.Success)
        {
            ViewBag.LoadError = result.Error;
            return View(new List<EventItem>());
        }

        var mine = OrganiserController.OwnEvents(result.Data!, Session.UserId)
            .OrderBy(e => e.Date).ToList();
        return View(mine);
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new EventForm { Date = DateTime.Today.AddDays(30).AddHours(7) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventForm form, IFormFile? banner)
    {
        Validate(form, banner, isNew: true);
        if (!ModelState.IsValid) return View("Form", form);

        var created = await Api.CreateEventAsync(form);
        if (!created.Success)
        {
            ModelState.AddModelError(string.Empty, created.Error ?? "The event could not be created.");
            return View("Form", form);
        }

        if (banner is { Length: > 0 } && created.Data > 0)
        {
            var upload = await Api.UploadEventBannerAsync(created.Data, banner);
            if (!upload.Success)
            {
                Error($"The event was created, but the banner upload failed: {upload.Error}");
                return RedirectToAction(nameof(Edit), new { id = created.Data });
            }
        }

        Success("Event created. Add categories next so participants can enter.");
        return RedirectToAction("Index", "ManageCategories", new { eventId = created.Data > 0 ? created.Data : (int?)null });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await Api.GetEventAsync(id);
        if (!result.Success)
        {
            Error(result.Error ?? "That event could not be loaded.");
            return RedirectToAction(nameof(Index));
        }

        var e = result.Data!;
        if (!Owns(e))
        {
            Error("You can only edit events that you created.");
            return RedirectToAction(nameof(Index));
        }

        return View("Form", new EventForm
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            Location = e.Location,
            Date = e.Date.Year > 1 ? e.Date : null,
            DistanceKm = e.DistanceKm > 0 ? e.DistanceKm : null,
            EventType = string.IsNullOrWhiteSpace(e.EventType) ? "Run" : e.EventType,
            CurrentBannerUrl = e.BannerUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventForm form, IFormFile? banner)
    {
        form.Id = id;
        Validate(form, banner, isNew: false);
        if (!ModelState.IsValid) return View("Form", form);

        var updated = await Api.UpdateEventAsync(id, form);
        if (!updated.Success)
        {
            ModelState.AddModelError(string.Empty, updated.Error ?? "The event could not be saved.");
            return View("Form", form);
        }

        if (banner is { Length: > 0 })
        {
            var upload = await Api.UploadEventBannerAsync(id, banner);
            if (!upload.Success)
            {
                Error($"The event was saved, but the banner upload failed: {upload.Error}");
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        Success("Event saved.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteEventAsync(id);
        if (result.Success) Success("Event deleted.");
        else Error(result.Error ?? "The event could not be deleted. Events with enrolments may need to be cleared first.");
        return RedirectToAction(nameof(Index));
    }

    private bool Owns(EventItem e) =>
        e.OrganiserId == 0 || !int.TryParse(Session.UserId, out var uid) || uid == e.OrganiserId;

    private void Validate(EventForm form, IFormFile? banner, bool isNew)
    {
        if (isNew && form.Date.HasValue && form.Date.Value < DateTime.Now)
        {
            ModelState.AddModelError(nameof(form.Date), "Choose a date and time in the future.");
        }

        var imageProblem = Format.ValidateImage(banner);
        if (imageProblem is not null) ModelState.AddModelError("banner", imageProblem);
    }
}
