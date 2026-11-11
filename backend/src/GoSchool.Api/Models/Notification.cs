namespace GoSchool.Api.Models;

// shortcut: in-app only (clients poll GET /api/notifications); wire FCM push on top when real-time delivery is needed
public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}
