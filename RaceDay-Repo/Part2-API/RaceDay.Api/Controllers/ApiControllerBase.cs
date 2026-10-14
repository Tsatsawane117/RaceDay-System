using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    protected string CurrentRole => HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;

    protected ObjectResult Fail(int status, string message) =>
        StatusCode(status, new ErrorResponse(status, message));
}
