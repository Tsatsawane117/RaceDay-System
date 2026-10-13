using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Security;


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _roles;

    public SessionAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        await session.LoadAsync();

        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);

        if (userId == null || role == null)
        {
            context.Result = new ObjectResult(new ErrorResponse(401, "You must be logged in to access this resource."))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(role))
        {
            context.Result = new ObjectResult(new ErrorResponse(403, $"This action is restricted to: {string.Join(", ", _roles)}."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
