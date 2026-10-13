namespace RaceDay.Api.Models;

public class Payment
{
    public int PaymentID { get; set; }
    public int EnrolmentID { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string PaymentMethod { get; set; } = "Card";
    public string PaymentStatus { get; set; } = "Pending";

    public Enrolment Enrolment { get; set; } = null!;
}
