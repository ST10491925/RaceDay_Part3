using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Role = "Organiser")]
public class ManageCategoriesController : AppController
{
    public ManageCategoriesController(IRaceDayApi api, IUserSession session) : base(api, session) { }

    [HttpGet]
    public async Task<IActionResult> Index(int? eventId)
    {
        var vm = await BuildAsync(eventId, new CategoryForm());
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int? eventId, [Bind(Prefix = "NewCategory")] CategoryForm form)
    {
        if (form.CategoryType == "Age")
        {
            if (form.MinAge is null && form.MaxAge is null)
                ModelState.AddModelError("NewCategory." + nameof(form.MinAge), "Enter a minimum age, a maximum age, or both.");
            else if (form.MinAge.HasValue && form.MaxAge.HasValue && form.MinAge > form.MaxAge)
                ModelState.AddModelError("NewCategory." + nameof(form.MaxAge), "Maximum age must be at least the minimum age.");
            form.DistanceKm = null;
        }
        else
        {
            if (form.DistanceKm is null)
                ModelState.AddModelError("NewCategory." + nameof(form.DistanceKm), "Enter the distance for this category.");
            form.MinAge = null;
            form.MaxAge = null;
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), await BuildAsync(eventId, form));
        }

        var created = await Api.CreateCategoryAsync(form);
        if (!created.Success)
        {
            ModelState.AddModelError(string.Empty, created.Error ?? "The category could not be created.");
            return View(nameof(Index), await BuildAsync(eventId, form));
        }

        if (eventId is > 0 && created.Data > 0)
        {
            var link = await Api.AddCategoryToEventAsync(eventId.Value, created.Data);
            if (!link.Success)
            {
                Error($"The category was created, but it could not be added to the event: {link.Error}");
                return RedirectToAction(nameof(Index), new { eventId });
            }
            Success("Category created and added to the event.");
        }
        else
        {
            Success("Category created.");
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Attach(int eventId, int categoryId)
    {
        if (eventId <= 0 || categoryId <= 0)
        {
            Error("Choose an event and a category.");
            return RedirectToAction(nameof(Index), new { eventId = eventId > 0 ? eventId : (int?)null });
        }

        var result = await Api.AddCategoryToEventAsync(eventId, categoryId);
        if (result.Success) Success("Category added to the event.");
        else Error(result.Error ?? "The category could not be added. It may already be on this event.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<CategoryManagementViewModel> BuildAsync(int? eventId, CategoryForm form)
    {
        var vm = new CategoryManagementViewModel { NewCategory = form, SelectedEventId = eventId };

        var events = await Api.GetEventsAsync();
        if (events.Success)
        {
            vm.Events = OrganiserController.OwnEvents(events.Data!, Session.UserId)
                .OrderBy(e => e.Date).ToList();
        }
        else
        {
            ViewBag.LoadError = events.Error;
        }

        var all = await Api.GetCategoriesAsync();
        if (all.Success) vm.AllCategories = all.Data!;

        if (eventId is > 0)
        {
            var linked = await Api.GetEventCategoriesAsync(eventId.Value);
            if (linked.Success) vm.EventCategories = linked.Data!;
        }

        return vm;
    }
}
