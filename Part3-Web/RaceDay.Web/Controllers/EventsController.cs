using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController : BaseController
{
    public EventsController(IRaceDayApi api) : base(api)
    {
    }

    public async Task<IActionResult> Index(string? search, string? type, int? maxDistance, bool showPast = false)
    {
        var vm = new EventsIndexVm { Search = search, Type = type, MaxDistance = maxDistance, ShowPast = showPast };

        var result = await Api.GetEventsAsync(type);
        if (!result.Success)
        {
            Failure(result.Error);
            return View(vm);
        }

        var events = result.Data ?? new List<EventDto>();

        if (!showPast)
        {
            events = events
                .Where(e => e.EventDate.Date >= DateTime.Today && e.Status != "Cancelled" && e.Status != "Completed")
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            events = events
                .Where(e => e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase)
                         || e.Location.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (maxDistance.HasValue)
        {
            events = events.Where(e => e.DistanceKM <= maxDistance.Value).ToList();
        }

        vm.Events = events.OrderBy(e => e.EventDate).ToList();
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var result = await Api.GetEventAsync(id);
        if (result.StatusCode == 404)
        {
            return NotFound();
        }

        if (!result.Success)
        {
            Failure(result.Error);
            return RedirectToAction(nameof(Index));
        }

        var vm = new EventDetailsVm { Event = result.Data! };

        // For a logged-in Participant, remember which categories they have already entered.
        if (CurrentRole == "Participant")
        {
            var mine = await Api.GetMyEnrolmentsAsync();
            if (SessionExpired(mine))
            {
                return ToLogin();
            }

            if (mine.Success)
            {
                vm.EnrolledCategoryIds = mine.Data!
                    .Where(e => e.Status != "Cancelled")
                    .Select(e => e.CategoryID)
                    .ToHashSet();
            }
        }

        return View(vm);
    }

    [HttpPost]
    [SessionAuth("Participant")]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var result = await Api.EnrolAsync(categoryId);
        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (!result.Success)
        {
            Failure(result.Error);
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        Success($"You're in! Your bib number is {result.Data!.BibNumber}. Pay your entry fee below to confirm your place.");
        return RedirectToAction("MyEvents", "Participant");
    }

    public async Task<IActionResult> Results(int id)
    {
        var eventResult = await Api.GetEventAsync(id);
        if (eventResult.StatusCode == 404)
        {
            return NotFound();
        }

        var resultsResult = await Api.GetEventResultsAsync(id);
        if (!eventResult.Success || !resultsResult.Success)
        {
            Failure(eventResult.Success ? resultsResult.Error : eventResult.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(new EventResultsVm
        {
            Event = eventResult.Data!,
            Results = resultsResult.Data!.Where(r => r.Status != "Not Started").ToList()
        });
    }
}
