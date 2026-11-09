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

    // Driver-only bank details
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? BankAccountNumber { get; set; }

    public Vehicle? Vehicle { get; set; }
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<DriverRoute> Routes { get; set; } = new List<DriverRoute>();
}
