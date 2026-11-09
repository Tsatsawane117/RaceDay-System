using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuth("Organiser")]
public class OrganiserController : BaseController
{
    public OrganiserController(IRaceDayApi api) : base(api)
    {
    }

    public async Task<IActionResult> Dashboard()
    {
        var result = await Api.GetMyEventsAsync();
        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (!result.Success)
        {
            Failure(result.Error);
            return View(new DashboardVm());
        }

        var events = result.Data!;
        return View(new DashboardVm
        {
            Events = events,
            TotalEnrolments = events.Sum(e => e.TotalEnrolments),
            Upcoming = events
                .Where(e => e.EventDate.Date >= DateTime.Today && e.Status != "Cancelled" && e.Status != "Completed")
                .OrderBy(e => e.EventDate)
                .ToList()
        });
    }

    // events

    public async Task<IActionResult> Events()
    {
        var result = await Api.GetMyEventsAsync();
        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (!result.Success)
        {
            Failure(result.Error);
            return View(new List<EventSummaryDto>());
        }

        return View(result.Data!);
    }

    [HttpGet]
    public IActionResult CreateEvent() =>
        View("EventForm", new EventFormVm { EventDate = DateTime.Today.AddMonths(1) });

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            return View("EventForm", vm);
        }

        var created = await Api.CreateEventAsync(ToRequest(vm));
        if (SessionExpired(created))
        {
            return ToLogin();
        }

        if (!created.Success)
        {
            ModelState.AddModelError(string.Empty, created.Error);
            return View("EventForm", vm);
        }

        await UploadBannerIfProvided(created.Data!.EventID, vm.BannerFile);
        Success($"'{created.Data.EventName}' has been created. Add categories so participants can enter.");
        return RedirectToAction(nameof(Categories), new { eventId = created.Data.EventID });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var ev = await LoadOwnedEventAsync(id);
        if (ev == null)
        {
            return RedirectToAction(nameof(Events));
        }

        return View("EventForm", new EventFormVm
        {
            EventId = ev.EventID,
            EventName = ev.EventName,
            EventDate = ev.EventDate,
            Location = ev.Location,
            Description = ev.Description,
            DistanceKM = ev.DistanceKM,
            EventType = ev.EventType,
            Status = ev.Status,
            ExistingBannerUrl = ev.BannerUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(int id, EventFormVm vm)
    {
        vm.EventId = id;
        if (!ModelState.IsValid)
        {
            return View("EventForm", vm);
        }

        var updated = await Api.UpdateEventAsync(id, ToRequest(vm));
        if (SessionExpired(updated))
        {
            return ToLogin();
        }

        if (!updated.Success)
        {
            ModelState.AddModelError(string.Empty, updated.Error);
            return View("EventForm", vm);
        }

        await UploadBannerIfProvided(id, vm.BannerFile);
        Success("The event has been updated.");
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await Api.DeleteEventAsync(id);
        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (result.Success)
        {
            Success("The event has been deleted.");
        }
        else
        {
            Failure(result.Error);
        }

        return RedirectToAction(nameof(Events));
    }

    //  categories

    [HttpGet]
    public async Task<IActionResult> Categories(int eventId)
    {
        var ev = await LoadOwnedEventAsync(eventId);
        if (ev == null)
        {
            return RedirectToAction(nameof(Events));
        }

        return View(new CategoriesVm { Event = ev, Form = new CategoryFormVm { EventId = eventId, DistanceKM = ev.DistanceKM } });
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(CategoryFormVm form)
    {
        if (!ModelState.IsValid)
        {
            var ev = await LoadOwnedEventAsync(form.EventId);
            if (ev == null)
            {
                return RedirectToAction(nameof(Events));
            }

            return View("Categories", new CategoriesVm { Event = ev, Form = form });
        }

        var result = await Api.AddCategoryAsync(form.EventId, new CategoryRequest
        {
            CategoryName = form.CategoryName.Trim(),
            DistanceKM = form.DistanceKM,
            MaxParticipants = form.MaxParticipants,
            EntryFee = form.EntryFee
        });

        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (result.Success)
        {
            Success($"Category '{result.Data!.CategoryName}' added.");
        }
        else
        {
            Failure(result.Error);
        }

        return RedirectToAction(nameof(Categories), new { eventId = form.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
    {
        var result = await Api.DeleteCategoryAsync(categoryId);
        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (result.Success)
        {
            Success("The category has been removed.");
        }
        else
        {
            Failure(result.Error);
        }

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    //  enrolments

    public async Task<IActionResult> Enrolments(int eventId)
    {
        var ev = await LoadOwnedEventAsync(eventId);
        if (ev == null)
        {
            return RedirectToAction(nameof(Events));
        }

        var result = await Api.GetEventEnrolmentsAsync(eventId);
        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (!result.Success)
        {
            Failure(result.Error);
        }

        return View(new EventEnrolmentsVm { Event = ev, Enrolments = result.Data ?? new List<EnrolmentDto>() });
    }

    //  results entry

    [HttpGet]
    public async Task<IActionResult> Results(int eventId)
    {
        var ev = await LoadOwnedEventAsync(eventId);
        if (ev == null)
        {
            return RedirectToAction(nameof(Events));
        }

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
        if (SessionExpired(enrolments))
        {
            return ToLogin();
        }

        var results = await Api.GetEventResultsAsync(eventId);
        if (!enrolments.Success || !results.Success)
        {
            Failure(enrolments.Success ? results.Error : enrolments.Error);
            return RedirectToAction(nameof(Events));
        }

        var existing = results.Data!.ToDictionary(r => r.EnrolmentID);

        var rows = enrolments.Data!
            .Where(e => e.Status != "Cancelled")
            .OrderBy(e => e.CategoryName)
            .ThenBy(e => e.ParticipantName)
            .Select(e =>
            {
                existing.TryGetValue(e.EnrolmentID, out var result);
                return new ResultRowVm
                {
                    EnrolmentID = e.EnrolmentID,
                    ParticipantName = e.ParticipantName ?? "Participant",
                    BibNumber = e.BibNumber,
                    CategoryName = e.CategoryName ?? string.Empty,
                    FinishTime = result?.FinishTime?.ToString(@"hh\:mm\:ss"),
                    Position = result?.Position,
                    Status = result?.Status ?? "Not Started"
                };
            })
            .ToList();

        return View(new ResultsEntryVm { Event = ev, Rows = rows });
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(int eventId, int enrolmentId, string? finishTime, int? position, string status)
    {
        TimeSpan? time = null;
        if (!string.IsNullOrWhiteSpace(finishTime))
        {
            if (!TimeSpan.TryParse(finishTime, out var parsed) || parsed <= TimeSpan.Zero)
            {
                Failure("Enter the finish time as hours:minutes:seconds, for example 01:45:30.");
                return RedirectToAction(nameof(Results), new { eventId });
            }

            time = parsed;
        }

        var result = await Api.CaptureResultAsync(new ResultRequest
        {
            EnrolmentID = enrolmentId,
            FinishTime = time,
            Position = position,
            Status = status
        });

        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (result.Success)
        {
            Success("Result saved and published.");
        }
        else
        {
            Failure(result.Error);
        }

        return RedirectToAction(nameof(Results), new { eventId });
    }

    //  helpers

    private async Task<EventDto?> LoadOwnedEventAsync(int id)
    {
        var result = await Api.GetEventAsync(id);
        if (!result.Success)
        {
            Failure(result.Error);
            return null;
        }

        if (result.Data!.OrganiserID != CurrentUserId)
        {
            Failure("You can only manage events that you organise.");
            return null;
        }

        return result.Data;
    }

    private async Task UploadBannerIfProvided(int eventId, IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return;
        }

        // The image goes to the API, which stores it in Azure Blob Storage and saves the URL on the event.
        var upload = await Api.UploadBannerAsync(eventId, file);
        if (!upload.Success)
        {
            Failure($"The event was saved, but the banner could not be uploaded: {upload.Error}");
        }
    }

    private static EventRequest ToRequest(EventFormVm vm) => new()
    {
        EventName = vm.EventName.Trim(),
        EventDate = vm.EventDate,
        Location = vm.Location.Trim(),
        Description = vm.Description?.Trim(),
        DistanceKM = vm.DistanceKM,
        EventType = vm.EventType,
        Status = vm.Status
    };
}
