namespace GoSchool.Api.Models;

public class DriverSubscription
{
    public int Id { get; set; }

    public int DriverId { get; set; }
    public User Driver { get; set; } = null!;

    public int BillingYear { get; set; }
    public int BillingMonth { get; set; }
    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }
}
