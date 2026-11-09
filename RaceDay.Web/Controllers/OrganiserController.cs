using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Role = "Organiser")]
public class OrganiserController : AppController
{
    public OrganiserController(IRaceDayApi api, IUserSession session) : base(api, session) { }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var vm = new DashboardViewModel();

        var events = await Api.GetEventsAsync();
        if (!events.Success)
        {
            ViewBag.LoadError = events.Error;
            return View(vm);
        }

        var mine = OwnEvents(events.Data!, Session.UserId);

        var counts = await Task.WhenAll(mine.Select(async e =>
        {
            var enrolments = await Api.GetEventEnrolmentsAsync(e.Id);
            var active = enrolments.Success
                ? enrolments.Data!.Count(x => !string.Equals(x.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                : 0;
            return new DashboardRow { Event = e, Enrolments = active };
        }));

        vm.Rows = counts.OrderBy(r => r.Event.Date).ToList();
        vm.TotalEvents = vm.Rows.Count;
        vm.TotalEnrolments = vm.Rows.Sum(r => r.Enrolments);
        return View(vm);
    }

    /// <summary>Events created by this organiser. Falls back to all events if the API omits the owner id.</summary>
    public static List<EventItem> OwnEvents(IEnumerable<EventItem> all, string? userId)
    {
        var list = all.ToList();
        if (!int.TryParse(userId, out var id) || id <= 0) return list;
        if (list.All(e => e.OrganiserId == 0)) return list;
        return list.Where(e => e.OrganiserId == id).ToList();
    }
}
