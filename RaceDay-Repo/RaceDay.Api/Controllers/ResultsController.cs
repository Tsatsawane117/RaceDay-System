using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/results")]
public class ResultsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CaptureResult(ResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentID == request.EnrolmentID!.Value);

        if (enrolment == null)
        {
            return Fail(404, $"Enrolment {request.EnrolmentID} was not found.");
        }

        if (enrolment.Category.Event.OrganiserID != CurrentUserId)
        {
            return Fail(403, "You can only capture results for events that you organise.");
        }

        if (enrolment.Status == "Cancelled")
        {
            return Fail(400, "Results cannot be captured for a cancelled enrolment.");
        }

        if (request.FinishTime.HasValue && request.FinishTime.Value <= TimeSpan.Zero)
        {
            return Fail(400, "FinishTime must be greater than zero.");
        }

        if (request.Status == "Finished" && (!request.FinishTime.HasValue || !request.Position.HasValue))
        {
            return Fail(400, "A 'Finished' result must include both FinishTime and Position.");
        }

        var result = await _db.Results.FirstOrDefaultAsync(r => r.EnrolmentID == enrolment.EnrolmentID);
        var isNew = result == null;

        if (result == null)
        {
            result = new Result { EnrolmentID = enrolment.EnrolmentID };
            _db.Results.Add(result);
        }

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        result.Status = request.Status;

        await _db.SaveChangesAsync();

        result.Enrolment = enrolment;
        var dto = ResultDto.From(result);

        return isNew ? StatusCode(StatusCodes.Status201Created, dto) : Ok(dto);
    }
}
