using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordService _hasher;

    public AuthController(RaceDayDbContext db, IPasswordService hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    /// <param name="request">FullName, Email, Password (min 8 chars), Role ("Organiser" or "Participant") and optional ContactNumber.</param>
    /// <response code="201">Account created. Returns the new user profile (no password).</response>
    /// <response code="400">A field is missing or invalid (for example an unknown role).</response>
    /// <response code="409">That email address is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return Fail(409, "An account with that email address already exists.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            Role = request.Role,
            ContactNumber = request.ContactNumber?.Trim()
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created("/api/users/me", UserDto.From(user));
    }
    /// <param name="request">Email and Password.</param>
    /// <response code="200">Login successful. Returns the user id, name and role.</response>
    /// <response code="400">Email or password missing.</response>
    /// <response code="401">Email or password is incorrect.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            return Fail(401, "Invalid email or password.");
        }

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserID);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);

        return Ok(new LoginResponse
        {
            UserID = user.UserID,
            FullName = user.FullName,
            Role = user.Role,
            Message = "Login successful."
        });
    }

   
    /// <response code="200">Session ended.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new MessageResponse { Message = "Logged out." });
    }
}
