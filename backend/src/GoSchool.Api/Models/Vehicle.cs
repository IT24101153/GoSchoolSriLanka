namespace GoSchool.Api.Models;

public class Vehicle
{
    public int Id { get; set; }

    public int DriverId { get; set; }
    public User Driver { get; set; } = null!;

    public string RegistrationNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public List<string> PhotoUrls { get; set; } = new();

    public AccountStatus ApprovalStatus { get; set; } = AccountStatus.Pending;
}
