using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public record ErrorResponse(int Code, string Message);

public class MessageResponse
{
    public string Message { get; set; } = string.Empty;
}

public class LoginResponse
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class UserDto
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public DateTime CreatedAt { get; set; }

    public static UserDto From(User u) => new()
    {
        UserID = u.UserID,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role,
        ContactNumber = u.ContactNumber,
        CreatedAt = u.CreatedAt
    };
}

public class CategoryDto
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }

    public static CategoryDto From(Category c) => new()
    {
        CategoryID = c.CategoryID,
        EventID = c.EventID,
        CategoryName = c.CategoryName,
        DistanceKM = c.DistanceKM,
        MaxParticipants = c.MaxParticipants,
        EntryFee = c.EntryFee
    };
}

public class EventDto
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DistanceKM { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<CategoryDto>? Categories { get; set; }

    public static EventDto From(Event e, bool includeCategories = false) => new()
    {
        EventID = e.EventID,
        OrganiserID = e.OrganiserID,
        EventName = e.EventName,
        EventDate = e.EventDate,
        Location = e.Location,
        Description = e.Description,
        DistanceKM = e.DistanceKM,
        EventType = e.EventType,
        Status = e.Status,
        Categories = includeCategories ? e.Categories.Select(CategoryDto.From).ToList() : null
    };
}

public class EnrolmentDto
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public string? ParticipantName { get; set; }
    public int CategoryID { get; set; }
    public string? CategoryName { get; set; }
    public int EventID { get; set; }
    public string? EventName { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string? BibNumber { get; set; }
    public string Status { get; set; } = string.Empty;

    public static EnrolmentDto From(Enrolment e) => new()
    {
        EnrolmentID = e.EnrolmentID,
        ParticipantID = e.ParticipantID,
        ParticipantName = e.Participant?.FullName,
        CategoryID = e.CategoryID,
        CategoryName = e.Category?.CategoryName,
        EventID = e.Category?.EventID ?? 0,
        EventName = e.Category?.Event?.EventName,
        EnrolmentDate = e.EnrolmentDate,
        BibNumber = e.BibNumber,
        Status = e.Status
    };
}

public class ResultDto
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public string? ParticipantName { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;

    public static ResultDto From(Result r) => new()
    {
        ResultID = r.ResultID,
        EnrolmentID = r.EnrolmentID,
        ParticipantName = r.Enrolment?.Participant?.FullName,
        FinishTime = r.FinishTime,
        Position = r.Position,
        Status = r.Status
    };
}

public class PaymentDto
{
    public int PaymentID { get; set; }
    public int EnrolmentID { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;

    public static PaymentDto From(Payment p) => new()
    {
        PaymentID = p.PaymentID,
        EnrolmentID = p.EnrolmentID,
        Amount = p.Amount,
        PaymentDate = p.PaymentDate,
        PaymentMethod = p.PaymentMethod,
        PaymentStatus = p.PaymentStatus
    };
}
