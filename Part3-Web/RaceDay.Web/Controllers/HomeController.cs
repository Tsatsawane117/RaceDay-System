using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : BaseController
{
    public HomeController(IRaceDayApi api) : base(api)
    {
    }

    [Route("Home/Error")]
    public IActionResult Error(int? code)
    {
        Response.StatusCode = code ?? 500;
        ViewData["Code"] = code ?? 500;
        return View();
    }
}
