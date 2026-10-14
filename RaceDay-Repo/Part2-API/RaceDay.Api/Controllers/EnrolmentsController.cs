using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/enrolments")]
[SessionAuthorize]
public class EnrolmentsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private Task<Enrolment?> LoadEnrolmentAsync(int id) =>
        _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentID == id);

    // The owning Participant, or the Organiser of the event, may access an enrolment.
    private bool CanAccess(Enrolment e) =>
        e.ParticipantID == CurrentUserId || e.Category.Event.OrganiserID == CurrentUserId;

   
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await LoadEnrolmentAsync(id);
        if (enrolment == null)
        {
            return Fail(404, $"Enrolment {id} was not found.");
        }

        if (!CanAccess(enrolment))
        {
            return Fail(403, "You can only cancel your own enrolments, or enrolments for events you organise.");
        }

        enrolment.Status = "Cancelled";
        await _db.SaveChangesAsync();

        return Ok(new MessageResponse { Message = $"Enrolment {id} cancelled." });
    }

    [HttpGet("{id:int}/results")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResult(int id)
    {
        var enrolment = await LoadEnrolmentAsync(id);
        if (enrolment == null)
        {
            return Fail(404, $"Enrolment {id} was not found.");
        }

        if (!CanAccess(enrolment))
        {
            return Fail(403, "You can only view results for your own enrolments, or for events you organise.");
        }

        var result = await _db.Results.AsNoTracking().FirstOrDefaultAsync(r => r.EnrolmentID == id);
        if (result == null)
        {
            return Fail(404, "No result has been recorded for this enrolment yet.");
        }

        result.Enrolment = enrolment;
        return Ok(ResultDto.From(result));
    }

    [HttpPost("{id:int}/payments")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddPayment(int id, PaymentRequest request)
    {
        var enrolment = await LoadEnrolmentAsync(id);
        if (enrolment == null)
        {
            return Fail(404, $"Enrolment {id} was not found.");
        }

        if (enrolment.ParticipantID != CurrentUserId)
        {
            return Fail(403, "You can only pay for your own enrolments.");
        }

        if (enrolment.Status == "Cancelled")
        {
            return Fail(409, "This enrolment has been cancelled.");
        }

        if (await _db.Payments.AnyAsync(p => p.EnrolmentID == id && p.PaymentStatus == "Success"))
        {
            return Fail(409, "This enrolment has already been paid for.");
        }

        var fee = enrolment.Category.EntryFee;
        if (request.Amount!.Value != fee)
        {
            return Fail(400, $"The amount must equal the entry fee of {fee:0.00}.");
        }

        var payment = new Payment
        {
            EnrolmentID = id,
            Amount = request.Amount.Value,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = "Success"
        };

        enrolment.Status = "Confirmed";
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, PaymentDto.From(payment));
    }

    [HttpGet("{id:int}/payments")]
    [ProducesResponseType(typeof(IEnumerable<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayments(int id)
    {
        var enrolment = await LoadEnrolmentAsync(id);
        if (enrolment == null)
        {
            return Fail(404, $"Enrolment {id} was not found.");
        }

        if (!CanAccess(enrolment))
        {
            return Fail(403, "You can only view payments for your own enrolments, or for events you organise.");
        }

        var payments = await _db.Payments.AsNoTracking()
            .Where(p => p.EnrolmentID == id)
            .OrderBy(p => p.PaymentID)
            .ToListAsync();

        return Ok(payments.Select(PaymentDto.From));
    }
}
