using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : BaseController
{
    public AccountController(IRaceDayApi api) : base(api)
    {
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) != null)
        {
            return RedirectToHome();
        }

        if (!string.IsNullOrEmpty(returnUrl) && TempData["Error"] == null)
        {
            TempData["Error"] = "Please log in to see that page.";
        }

        return View(new LoginVm { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var result = await Api.LoginAsync(vm.Email.Trim(), vm.Password);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(vm);
        }

        var user = result.Data!;
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserID);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);
        HttpContext.Session.SetString(SessionKeys.FullName, user.FullName);

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
        {
            return LocalRedirect(vm.ReturnUrl);
        }

        Success($"Welcome back, {user.FullName}!");
        return RedirectToHome();
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) != null)
        {
            return RedirectToHome();
        }

        return View(new RegisterVm());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var result = await Api.RegisterAsync(new RegisterRequest
        {
            FullName = vm.FullName.Trim(),
            Email = vm.Email.Trim(),
            Password = vm.Password,
            Role = vm.Role,
            ContactNumber = vm.ContactNumber
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(vm);
        }

        Success("Your account has been created. Please log in.");
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        // End the session on the API, then clear the web session either way.
        await Api.LogoutAsync();
        HttpContext.Session.Clear();

        Success("You have been logged out.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToHome() =>
        CurrentRole == "Organiser"
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Events");
}
