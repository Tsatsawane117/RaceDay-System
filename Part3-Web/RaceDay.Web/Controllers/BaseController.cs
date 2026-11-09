using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public abstract class BaseController : Controller
{
    protected BaseController(IRaceDayApi api)
    {
        Api = api;
    }

    protected IRaceDayApi Api { get; }

    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    protected string CurrentRole => HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;

    protected void Success(string message) => TempData["Success"] = message;

    protected void Failure(string message) => TempData["Error"] = message;


    protected bool SessionExpired<T>(ApiResult<T> result)
    {
        if (result.StatusCode != 401)
        {
            return false;
        }

        HttpContext.Session.Clear();
        Failure("Your session has expired. Please log in again.");
        return true;
    }

    protected IActionResult ToLogin() => RedirectToAction("Login", "Account");
}
