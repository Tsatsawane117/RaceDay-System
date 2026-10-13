using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/users")]
[SessionAuthorize]
public class UsersController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile()
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == CurrentUserId);
        if (user == null)
        {
            return Fail(401, "Your account no longer exists. Please log in again.");
        }

        return Ok(UserDto.From(user));
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == CurrentUserId);
        if (user == null)
        {
            return Fail(401, "Your account no longer exists. Please log in again.");
        }

        user.FullName = request.FullName.Trim();
        user.ContactNumber = request.ContactNumber?.Trim();
        await _db.SaveChangesAsync();

        return Ok(UserDto.From(user));
    }

    [HttpGet("me/enrolments")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var enrolments = await _db.Enrolments.AsNoTracking()
            .Where(e => e.ParticipantID == CurrentUserId)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .OrderBy(e => e.EnrolmentID)
            .ToListAsync();

        return Ok(enrolments.Select(EnrolmentDto.From));
    }
}
