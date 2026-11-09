using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Role = "Participant")]
public class ParticipantController : AppController
{
    public ParticipantController(IRaceDayApi api, IUserSession session) : base(api, session) { }

    [HttpGet]
    public async Task<IActionResult> MyEvents()
    {
        var result = await Api.GetMyEnrolmentsAsync();
        if (!result.Success)
        {
            ViewBag.LoadError = result.Error;
            return View(new List<EnrolmentItem>());
        }

        var list = result.Data!
            .OrderByDescending(e => e.EventDate ?? DateTime.MinValue)
            .ToList();
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int enrolmentId)
    {
        var result = await Api.CancelEnrolmentAsync(enrolmentId);
        if (result.Success) Success("Your enrolment has been cancelled.");
        else Error(result.Error ?? "The enrolment could not be cancelled.");
        return RedirectToAction(nameof(MyEvents));
    }

    [HttpGet]
    public async Task<IActionResult> MyResults()
    {
        var result = await Api.GetMyResultsAsync();
        if (!result.Success)
        {
            ViewBag.LoadError = result.Error;
            return View(new List<ResultItem>());
        }

        var results = result.Data!;

        // The API should include the total finishers. If it does not, try the event results.
        var missing = results.Where(r => r.TotalFinishers is null && r.EventId > 0)
                             .Select(r => r.EventId).Distinct().ToList();
        if (missing.Count > 0)
        {
            var totals = new Dictionary<int, int>();
            foreach (var eventId in missing)
            {
                var all = await Api.GetEventResultsAsync(eventId);
                if (all.Success && all.Data!.Count > 0) totals[eventId] = all.Data.Count;
            }
            foreach (var r in results.Where(r => r.TotalFinishers is null))
            {
                if (totals.TryGetValue(r.EventId, out var total)) r.TotalFinishers = total;
            }
        }

        return View(results.OrderByDescending(r => r.EventDate ?? DateTime.MinValue).ToList());
    }
}
