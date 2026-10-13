using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class RegisterRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Either "Organiser" or "Participant".</summary>
    [Required, RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be 'Organiser' or 'Participant'.")]
    public string Role { get; set; } = string.Empty;

    [StringLength(20)]
    public string? ContactNumber { get; set; }
}

public class LoginRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? ContactNumber { get; set; }
}

public class EventRequest
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    public DateTime? EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required, Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    public decimal? DistanceKM { get; set; }

    
    [Required, RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be 'Run', 'Walk' or 'Cycle'.")]
    public string EventType { get; set; } = string.Empty;

    [RegularExpression("^(Planned|Open|Closed|Completed|Cancelled)$", ErrorMessage = "Invalid status.")]
    public string? Status { get; set; }
}

public class CategoryRequest
{
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Required, Range(0.1, 1000)]
    public decimal? DistanceKM { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 100000)]
    public decimal EntryFee { get; set; }
}

public class ResultRequest
{
    [Required]
    public int? EnrolmentID { get; set; }


    public TimeSpan? FinishTime { get; set; }

    [Range(1, 100000)]
    public int? Position { get; set; }

    [Required, RegularExpression("^(Not Started|Finished|DNF|DSQ)$", ErrorMessage = "Status must be 'Not Started', 'Finished', 'DNF' or 'DSQ'.")]
    public string Status { get; set; } = string.Empty;
}

public class PaymentRequest
{
    [Required, Range(0.01, 100000)]
    public decimal? Amount { get; set; }

    [Required, RegularExpression("^(Card|EFT|Cash)$", ErrorMessage = "PaymentMethod must be 'Card', 'EFT' or 'Cash'.")]
    public string PaymentMethod { get; set; } = string.Empty;
}
