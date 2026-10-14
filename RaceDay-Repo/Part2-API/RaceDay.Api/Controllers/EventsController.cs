using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/events")]
public class EventsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        var ev = new Event
        {
            OrganiserID = CurrentUserId,
            EventName = request.EventName.Trim(),
            EventDate = request.EventDate!.Value.Date,
            Location = request.Location.Trim(),
            Description = request.Description?.Trim(),
            DistanceKM = request.DistanceKM!.Value,
            EventType = request.EventType,
            Status = request.Status ?? "Planned"
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, EventDto.From(ev));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents([FromQuery] string? status, [FromQuery] string? eventType)
    {
        var query = _db.Events.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(e => e.EventType == eventType);
        }

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(e => EventDto.From(e)));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _db.Events.AsNoTracking()
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev == null)
        {
            return Fail(404, $"Event {id} was not found.");
        }

        return Ok(EventDto.From(ev, includeCategories: true));
    }

    [HttpPut("{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, EventRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null)
        {
            return Fail(404, $"Event {id} was not found.");
        }

        if (ev.OrganiserID != CurrentUserId)
        {
            return Fail(403, "You can only change events that you organise.");
        }

        ev.EventName = request.EventName.Trim();
        ev.EventDate = request.EventDate!.Value.Date;
        ev.Location = request.Location.Trim();
        ev.Description = request.Description?.Trim();
        ev.DistanceKM = request.DistanceKM!.Value;
        ev.EventType = request.EventType;
        ev.Status = request.Status ?? ev.Status;

        await _db.SaveChangesAsync();
        return Ok(EventDto.From(ev));
    }

    [HttpDelete("{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null)
        {
            return Fail(404, $"Event {id} was not found.");
        }

        if (ev.OrganiserID != CurrentUserId)
        {
            return Fail(403, "You can only delete events that you organise.");
        }

        if (await _db.Enrolments.AnyAsync(e => e.Category.EventID == id))
        {
            return Fail(409, "This event already has enrolments and cannot be deleted. Set its status to 'Cancelled' instead.");
        }

        _db.Categories.RemoveRange(ev.Categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return Ok(new MessageResponse { Message = $"Event {id} deleted." });
    }


    [HttpPost("{id:int}/categories")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddCategory(int id, CategoryRequest request)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null)
        {
            return Fail(404, $"Event {id} was not found.");
        }

        if (ev.OrganiserID != CurrentUserId)
        {
            return Fail(403, "You can only add categories to events that you organise.");
        }

        var category = new Category
        {
            EventID = id,
            CategoryName = request.CategoryName.Trim(),
            DistanceKM = request.DistanceKM!.Value,
            MaxParticipants = request.MaxParticipants,
            EntryFee = request.EntryFee
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, CategoryDto.From(category));
    }

    [HttpGet("{id:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == id))
        {
            return Fail(404, $"Event {id} was not found.");
        }

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.EventID == id)
            .OrderBy(c => c.DistanceKM)
            .ToListAsync();

        return Ok(categories.Select(CategoryDto.From));
    }

    [HttpGet("{id:int}/enrolments")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventEnrolments(int id)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null)
        {
            return Fail(404, $"Event {id} was not found.");
        }

        if (ev.OrganiserID != CurrentUserId)
        {
            return Fail(403, "You can only view enrolments for events that you organise.");
        }

        var enrolments = await _db.Enrolments.AsNoTracking()
            .Where(e => e.Category.EventID == id)
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .OrderBy(e => e.EnrolmentID)
            .ToListAsync();

        return Ok(enrolments.Select(EnrolmentDto.From));
    }

    [HttpGet("{id:int}/results")]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventResults(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == id))
        {
            return Fail(404, $"Event {id} was not found.");
        }

        var results = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment.Category.EventID == id)
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .OrderBy(r => r.Position == null)
            .ThenBy(r => r.Position)
            .ToListAsync();

        return Ok(results.Select(ResultDto.From));
    }
}
