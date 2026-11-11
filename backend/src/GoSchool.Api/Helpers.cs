using System.Security.Claims;
using System.Security.Cryptography;
using GoSchool.Api.Data;
using GoSchool.Api.Models;

namespace GoSchool.Api;

public static class BillingPeriod
{
    private static readonly TimeZoneInfo Colombo = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

    public static (int Year, int Month) Current()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Colombo);
        return (now.Year, now.Month);
    }

    public static string SubscriptionStatus(DriverSubscription s)
    {
        if (s.Status == PaymentStatus.Completed) return "Completed";
        return (s.BillingYear, s.BillingMonth).CompareTo(Current()) < 0 ? "Overdue" : "Pending";
    }
}

public static class Geo
{
    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}

public static class Tokens
{
    public static string New() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}

public static class Extensions
{
    public static int UserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static void Notify(this GoSchoolDbContext db, int userId, string message) =>
        db.Notifications.Add(new Notification { UserId = userId, Message = message });
}

public static class Dto
{
    public static object User(User u) => new { u.Id, u.Role, u.FullName, u.Email, u.PhoneNumber, u.Status };

    public static object Vehicle(Vehicle v) => new { v.Id, v.RegistrationNumber, v.Capacity, v.PhotoUrls, v.ApprovalStatus };

    public static object Stop(RouteStop s) => new { s.Id, s.Sequence, s.School, s.PickupAddress, s.Latitude, s.Longitude, s.ArrivalTime };

    public static object Route(DriverRoute r) => new
    {
        r.Id, r.Name, r.PricePerDay, r.PricePerMonth,
        stops = r.Stops.OrderBy(s => s.Sequence).Select(Stop),
    };

    public static object Subscription(DriverSubscription s) => new
    {
        s.Id, s.DriverId, s.BillingYear, s.BillingMonth, s.Amount, s.PaidAt,
        status = BillingPeriod.SubscriptionStatus(s),
    };

    public static object Payment(Payment p) => new
    {
        p.Id, p.ApplicationId, p.BillingYear, p.BillingMonth, p.Amount, p.Method, p.Status, p.SlipImageUrl, p.PaidAt,
    };
}
