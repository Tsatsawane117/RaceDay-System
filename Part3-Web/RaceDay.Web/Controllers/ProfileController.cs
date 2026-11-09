using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuth]
public class ProfileController : BaseController
{
    public ProfileController(IRaceDayApi api) : base(api)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetProfileAsync();
        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (!result.Success)
        {
            Failure(result.Error);
            return RedirectToAction("Index", "Events");
        }

        return View(BuildPage(result.Data!));
    }

    [HttpPost]
    public async Task<IActionResult> Update(ProfileFormVm form)
    {
        if (!ModelState.IsValid)
        {
            var profile = await Api.GetProfileAsync();
            if (SessionExpired(profile))
            {
                return ToLogin();
            }

            return View("Index", new ProfilePageVm { User = profile.Data ?? new UserDto(), Form = form });
        }

        var result = await Api.UpdateProfileAsync(form.FullName.Trim(), form.ContactNumber?.Trim());
        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (result.Success)
        {
            // Keep the name shown in the navigation bar up to date.
            HttpContext.Session.SetString(SessionKeys.FullName, result.Data!.FullName);
            Success("Your details have been updated.");
        }
        else
        {
            Failure(result.Error);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Picture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            Failure("Please choose an image to upload.");
            return RedirectToAction(nameof(Index));
        }

        // The file goes to the API, which stores it in Azure Blob Storage.
        var result = await Api.UploadProfilePictureAsync(picture);
        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (result.Success)
        {
            Success("Your profile picture has been updated.");
        }
        else
        {
            Failure(result.Error);
        }

        return RedirectToAction(nameof(Index));
    }

    private static ProfilePageVm BuildPage(UserDto user) => new()
    {
        User = user,
        Form = new ProfileFormVm { FullName = user.FullName, ContactNumber = user.ContactNumber }
    };
}
