namespace GoSchool.Api.Models;

public class Payment
{
    public int Id { get; set; }

    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public int BillingYear { get; set; }
    public int BillingMonth { get; set; }
    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? SlipImageUrl { get; set; }

    public DateTime? PaidAt { get; set; }
}
