namespace GoSchool.Api.Models;

public class User
{
    public int Id { get; set; }
    public UserRole Role { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // shortcut: one token per user, so a new login signs out other devices; move to a token table when multi-device matters
    public string? ApiToken { get; set; }

    // Parent-only home location
    public string? HomeAddress { get; set; }
    public double? HomeLatitude { get; set; }
    public double? HomeLongitude { get; set; }

    // Driver-only bank details
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? BankAccountNumber { get; set; }

    public Vehicle? Vehicle { get; set; }
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<DriverRoute> Routes { get; set; } = new List<DriverRoute>();
    public ICollection<DriverSubscription> Subscriptions { get; set; } = new List<DriverSubscription>();
}
