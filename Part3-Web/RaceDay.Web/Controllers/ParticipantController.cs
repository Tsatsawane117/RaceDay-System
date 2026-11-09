using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;


[SessionAuth("Participant")]
public class ParticipantController : BaseController
{
    public ParticipantController(IRaceDayApi api) : base(api)
    {
    }
    public async Task<IActionResult> MyEvents()
    {
        var result = await Api.GetMyEnrolmentsAsync();
        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (!result.Success)
        {
            Failure(result.Error);
            return View(new List<EnrolmentDto>());
        }

        return View(result.Data!.OrderByDescending(e => e.EventDate).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Pay(int enrolmentId, string paymentMethod)
    {
        // The amount comes from the participant's own enrolment on the server, never from the browser.
        var mine = await Api.GetMyEnrolmentsAsync();
        if (SessionExpired(mine))
        {
            return ToLogin();
        }

        var enrolment = mine.Data?.FirstOrDefault(e => e.EnrolmentID == enrolmentId);
        if (!mine.Success || enrolment == null)
        {
            Failure(mine.Success ? "We could not find that enrolment." : mine.Error);
            return RedirectToAction(nameof(MyEvents));
        }

        var result = await Api.PayAsync(enrolmentId, enrolment.EntryFee, paymentMethod);
        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (result.Success)
        {
            Success("Payment received. Your place is confirmed!");
        }
        else
        {
            Failure(result.Error);
        }

        return RedirectToAction(nameof(MyEvents));
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int enrolmentId)
    {
        var result = await Api.CancelEnrolmentAsync(enrolmentId);
        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (result.Success)
        {
            Success("Your enrolment has been cancelled.");
        }
        else
        {
            Failure(result.Error);
        }

        return RedirectToAction(nameof(MyEvents));
    }

    public async Task<IActionResult> MyResults()
    {
        var result = await Api.GetMyResultsAsync();
        if (SessionExpired(result))
        {
            return ToLogin();
        }

        if (!result.Success)
        {
            Failure(result.Error);
            return View(new List<MyResultDto>());
        }

        return View(result.Data!);
    }
}
