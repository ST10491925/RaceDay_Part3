using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : AppController
{
    public HomeController(IRaceDayApi api, IUserSession session) : base(api, session) { }

    public async Task<IActionResult> Index(string? search, string? type, string? category, decimal? maxDistance)
    {
        var vm = new HomeViewModel
        {
            Search = search?.Trim(),
            Type = type,
            Category = category,
            MaxDistance = maxDistance is > 0 ? maxDistance : null
        };

        var eventsResult = await Api.GetEventsAsync();
        if (!eventsResult.Success)
        {
            ViewBag.LoadError = eventsResult.Error;
            return View(vm);
        }

        var categoriesResult = await Api.GetCategoriesAsync();
        if (categoriesResult.Success)
        {
            vm.Categories = categoriesResult.Data!
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(c => c.Name)
                .ToList();
        }

        var events = eventsResult.Data!.Where(e => e.IsUpcoming).ToList();

        if (!string.IsNullOrWhiteSpace(vm.Search))
        {
            var term = vm.Search;
            events = events.Where(e =>
                e.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(vm.Type))
        {
            events = events.Where(e => string.Equals(e.EventType, vm.Type, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (vm.MaxDistance.HasValue)
        {
            events = events.Where(e => e.DistanceKm <= vm.MaxDistance.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(vm.Category))
        {
            // Category links live on the event, so look them up for the events still in play.
            var lookups = await Task.WhenAll(events.Select(async e =>
            {
                var r = await Api.GetEventCategoriesAsync(e.Id);
                return (Event: e, Match: r.Success && r.Data!.Any(c =>
                    string.Equals(c.Name, vm.Category, StringComparison.OrdinalIgnoreCase)));
            }));
            events = lookups.Where(x => x.Match).Select(x => x.Event).ToList();
        }

        vm.Events = events.OrderBy(e => e.Date).ToList();
        return View(vm);
    }

    [Route("Home/Error")]
    public IActionResult Error(int? code)
    {
        var vm = new ErrorViewModel { Code = code };
        switch (code)
        {
            case 404:
                vm.Title = "Page not found";
                vm.Message = "That page does not exist. It may have moved or the link may be wrong.";
                break;
            case 403:
                vm.Title = "Access denied";
                vm.Message = "Your account does not have access to that page.";
                break;
            default:
                vm.Title = "Something went wrong";
                vm.Message = "The page could not be shown. Try again, or return to the home page.";
                break;
        }
        Response.StatusCode = code is >= 400 and < 600 ? code.Value : 500;
        return View(vm);
    }
}
