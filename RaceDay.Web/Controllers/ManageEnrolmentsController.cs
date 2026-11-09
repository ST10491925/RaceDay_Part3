using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Role = "Organiser")]
public class ManageEnrolmentsController : AppController
{
    public ManageEnrolmentsController(IRaceDayApi api, IUserSession session) : base(api, session) { }

    [HttpGet]
    public async Task<IActionResult> Index(int? eventId)
    {
        var vm = new EnrolmentManagementViewModel { SelectedEventId = eventId };

        var events = await Api.GetEventsAsync();
        if (!events.Success)
        {
            ViewBag.LoadError = events.Error;
            return View(vm);
        }

        vm.Events = OrganiserController.OwnEvents(events.Data!, Session.UserId)
            .OrderBy(e => e.Date).ToList();

        if (eventId is > 0)
        {
            var enrolments = await Api.GetEventEnrolmentsAsync(eventId.Value);
            if (enrolments.Success)
                vm.Enrolments = enrolments.Data!.OrderBy(e => e.ParticipantName).ToList();
            else
                ViewBag.LoadError = enrolments.Error;
        }

        return View(vm);
    }
}
