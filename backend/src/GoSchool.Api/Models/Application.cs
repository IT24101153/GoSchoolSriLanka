namespace GoSchool.Api.Models;

public class Application
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int RouteId { get; set; }
    public DriverRoute Route { get; set; } = null!;

    public int RouteStopId { get; set; }
    public RouteStop Stop { get; set; } = null!;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
