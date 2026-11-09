using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController : AppController
{
    public EventsController(IRaceDayApi api, IUserSession session) : base(api, session) { }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var ev = await Api.GetEventAsync(id);
        if (!ev.Success)
        {
            Error(ev.Status == 404 ? "That event could not be found." : ev.Error ?? "The event could not be loaded.");
            return RedirectToAction("Index", "Home");
        }

        var vm = new EventDetailViewModel { Event = ev.Data! };

        vm.Categories = vm.Event.Categories;
        if (vm.Categories.Count == 0)
        {
            var cats = await Api.GetEventCategoriesAsync(id);
            if (cats.Success) vm.Categories = cats.Data!;
        }

        if (Session.IsParticipant)
        {
            var mine = await Api.GetMyEnrolmentsAsync();
            if (mine.Success)
            {
                vm.MyEnrolment = mine.Data!.FirstOrDefault(e =>
                    e.EventId == id &&
                    !string.Equals(e.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));
            }
        }

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [SessionAuthorize(Role = "Participant")]
    public async Task<IActionResult> Enter(int eventId, int? eventCategoryId)
    {
        if (eventCategoryId is null or <= 0)
        {
            Error("Choose a category before entering the event.");
            return RedirectToAction(nameof(Detail), new { id = eventId });
        }

        var ev = await Api.GetEventAsync(eventId);
        if (ev.Success && !ev.Data!.IsUpcoming)
        {
            Error("This event has already taken place, so entries are closed.");
            return RedirectToAction(nameof(Detail), new { id = eventId });
        }

        var result = await Api.EnrolAsync(eventId, eventCategoryId.Value);
        if (result.Success)
            Success("You are entered. Your enrolment now appears under My Events.");
        else
            Error(result.Error ?? "The entry could not be recorded.");

        return RedirectToAction(nameof(Detail), new { id = eventId });
    }
}
