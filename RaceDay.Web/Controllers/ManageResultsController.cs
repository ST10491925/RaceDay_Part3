using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Role = "Organiser")]
public class ManageResultsController : AppController
{
    public ManageResultsController(IRaceDayApi api, IUserSession session) : base(api, session) { }

    [HttpGet]
    public async Task<IActionResult> Index(int? eventId)
    {
        return View(await BuildAsync(eventId));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ResultForm form)
    {
        var back = RedirectToAction(nameof(Index), new { eventId = form.EventId });

        if (!ModelState.IsValid)
        {
            Error(string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m))));
            return back;
        }

        if (!ResultForm.IsValidFinishTime(form.FinishTime))
        {
            Error("Enter the finish time as hh:mm:ss, for example 01:42:09.");
            return back;
        }

        var ev = await Api.GetEventAsync(form.EventId);
        if (ev.Success && ev.Data!.Date > DateTime.Now)
        {
            Error("Results can only be recorded after the event has concluded.");
            return back;
        }

        var existing = await Api.GetEventResultsAsync(form.EventId);
        if (existing.Success && existing.Data!.Any(r =>
                r.Position == form.Position && r.EnrolmentId != form.EnrolmentId))
        {
            Error($"Position {form.Position} is already taken by another participant.");
            return back;
        }

        form.FinishTime = form.FinishTime.Trim();
        var result = await Api.SaveResultAsync(form);
        if (result.Success) Success("Result saved and published.");
        else Error(result.Error ?? "The result could not be saved.");
        return back;
    }

    private async Task<ResultManagementViewModel> BuildAsync(int? eventId)
    {
        var vm = new ResultManagementViewModel();

        var events = await Api.GetEventsAsync();
        if (!events.Success)
        {
            ViewBag.LoadError = events.Error;
            return vm;
        }

        vm.Events = OrganiserController.OwnEvents(events.Data!, Session.UserId)
            .OrderByDescending(e => e.Date).ToList();

        if (eventId is not > 0) return vm;

        vm.SelectedEvent = vm.Events.FirstOrDefault(e => e.Id == eventId);
        if (vm.SelectedEvent is null) return vm;

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId.Value);
        var results = await Api.GetEventResultsAsync(eventId.Value);

        if (!enrolments.Success)
        {
            ViewBag.LoadError = enrolments.Error;
            return vm;
        }

        var resultList = results.Success ? results.Data! : new List<ResultItem>();

        vm.Rows = enrolments.Data!
            .Where(e => !string.Equals(e.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.ParticipantName)
            .Select(e => new ResultRow
            {
                Enrolment = e,
                Result = resultList.FirstOrDefault(r => r.EnrolmentId == e.Id)
            })
            .ToList();

        return vm;
    }
}
