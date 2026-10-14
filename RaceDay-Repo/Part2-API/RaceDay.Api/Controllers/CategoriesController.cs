using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/categories")]
public class CategoriesController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpPut("{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null)
        {
            return Fail(404, $"Category {id} was not found.");
        }

        if (category.Event.OrganiserID != CurrentUserId)
        {
            return Fail(403, "You can only change categories for events that you organise.");
        }

        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKM = request.DistanceKM!.Value;
        category.MaxParticipants = request.MaxParticipants;
        category.EntryFee = request.EntryFee;

        await _db.SaveChangesAsync();
        return Ok(CategoryDto.From(category));
    }

    [HttpDelete("{id:int}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null)
        {
            return Fail(404, $"Category {id} was not found.");
        }

        if (category.Event.OrganiserID != CurrentUserId)
        {
            return Fail(403, "You can only delete categories for events that you organise.");
        }

        if (await _db.Enrolments.AnyAsync(e => e.CategoryID == id))
        {
            return Fail(409, "This category already has enrolments and cannot be deleted.");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return Ok(new MessageResponse { Message = $"Category {id} deleted." });
    }


    [HttpPost("{id:int}/enrol")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null)
        {
            return Fail(404, $"Category {id} was not found.");
        }

        if (category.Event.Status != "Open")
        {
            return Fail(400, $"Event '{category.Event.EventName}' is not open for enrolment (status: {category.Event.Status}).");
        }

        var userId = CurrentUserId;
        var existing = await _db.Enrolments.FirstOrDefaultAsync(e => e.ParticipantID == userId && e.CategoryID == id);
        if (existing != null && existing.Status != "Cancelled")
        {
            return Fail(409, "You are already enrolled in this category.");
        }

        var activeCount = await _db.Enrolments.CountAsync(e => e.CategoryID == id && e.Status != "Cancelled");
        if (activeCount >= category.MaxParticipants)
        {
            return Fail(400, "This category is full.");
        }

        Enrolment enrolment;
        if (existing != null)
        {
            // Re-enrolling after a cancellation re-uses the original record (one row per participant + category).
            existing.Status = "Pending";
            existing.EnrolmentDate = DateTime.UtcNow;
            enrolment = existing;
        }
        else
        {
            enrolment = new Enrolment { ParticipantID = userId, CategoryID = id };
            _db.Enrolments.Add(enrolment);
        }

        await _db.SaveChangesAsync();

        if (string.IsNullOrEmpty(enrolment.BibNumber))
        {
            enrolment.BibNumber = $"B{1000 + enrolment.EnrolmentID}";
            await _db.SaveChangesAsync();
        }

        enrolment.Category = category;
        return StatusCode(StatusCodes.Status201Created, EnrolmentDto.From(enrolment));
    }
}
