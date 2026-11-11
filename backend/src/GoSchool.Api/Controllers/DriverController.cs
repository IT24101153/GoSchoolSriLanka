using System.ComponentModel.DataAnnotations;
using GoSchool.Api.Data;
using GoSchool.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoSchool.Api.Controllers;

[ApiController]
[Route("api/driver")]
[Authorize(Policy = "ActiveDriver")]
public class DriverController(GoSchoolDbContext db) : ControllerBase
{
    private int DriverId => User.UserId();

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var (year, month) = BillingPeriod.Current();
        var u = await db.Users.Include(x => x.Vehicle).FirstAsync(x => x.Id == DriverId);
        var sub = await db.DriverSubscriptions.FirstOrDefaultAsync(s => s.DriverId == DriverId && s.BillingYear == year && s.BillingMonth == month);
        return Ok(new
        {
            user = Dto.User(u),
            vehicle = u.Vehicle is null ? null : Dto.Vehicle(u.Vehicle),
            bank = new { u.BankName, u.BankBranch, u.BankAccountNumber },
            acceptedSeats = await db.Applications.CountAsync(a => a.Route.DriverId == DriverId && a.Status == ApplicationStatus.Accepted),
            pendingRequests = await db.Applications.CountAsync(a => a.Route.DriverId == DriverId && a.Status == ApplicationStatus.Pending),
            subscription = sub is null ? null : Dto.Subscription(sub),
        });
    }

    [HttpPut("bank")]
    public async Task<IActionResult> UpdateBank(BankDetailsRequest req)
    {
        var u = await db.Users.FirstAsync(x => x.Id == DriverId);
        u.BankName = req.BankName;
        u.BankBranch = req.BankBranch;
        u.BankAccountNumber = req.BankAccountNumber;
        await db.SaveChangesAsync();
        return Ok(new { u.BankName, u.BankBranch, u.BankAccountNumber });
    }

    // --- Routes & stops ---

    [HttpGet("routes")]
    public async Task<IActionResult> Routes()
    {
        var routes = await db.Routes.Where(r => r.DriverId == DriverId).Include(r => r.Stops).OrderBy(r => r.Name).ToListAsync();
        return Ok(routes.Select(Dto.Route));
    }

    [HttpPost("routes")]
    public async Task<IActionResult> CreateRoute(RouteRequest req)
    {
        var route = new DriverRoute { DriverId = DriverId, Name = req.Name, PricePerDay = req.PricePerDay, PricePerMonth = req.PricePerMonth };
        db.Routes.Add(route);
        await db.SaveChangesAsync();
        return Ok(Dto.Route(route));
    }

    [HttpPut("routes/{id:int}")]
    public async Task<IActionResult> UpdateRoute(int id, RouteRequest req)
    {
        var route = await OwnedRoute(id);
        if (route is null) return NotFound();
        route.Name = req.Name;
        route.PricePerDay = req.PricePerDay;
        route.PricePerMonth = req.PricePerMonth;
        await db.SaveChangesAsync();
        return Ok(Dto.Route(route));
    }

    [HttpDelete("routes/{id:int}")]
    public async Task<IActionResult> DeleteRoute(int id)
    {
        var route = await OwnedRoute(id);
        if (route is null) return NotFound();
        if (await db.Applications.AnyAsync(a => a.RouteId == id)) return Conflict(new { message = "Route has seat applications; it cannot be deleted" });
        db.Routes.Remove(route);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("routes/{id:int}/stops")]
    public async Task<IActionResult> AddStop(int id, StopRequest req)
    {
        var route = await OwnedRoute(id);
        if (route is null) return NotFound();
        var stop = new RouteStop { RouteId = id };
        Apply(stop, req);
        db.RouteStops.Add(stop);
        await db.SaveChangesAsync();
        return Ok(Dto.Stop(stop));
    }

    [HttpPut("stops/{id:int}")]
    public async Task<IActionResult> UpdateStop(int id, StopRequest req)
    {
        var stop = await OwnedStop(id);
        if (stop is null) return NotFound();
        Apply(stop, req);
        await db.SaveChangesAsync();
        return Ok(Dto.Stop(stop));
    }

    [HttpDelete("stops/{id:int}")]
    public async Task<IActionResult> DeleteStop(int id)
    {
        var stop = await OwnedStop(id);
        if (stop is null) return NotFound();
        if (await db.Applications.AnyAsync(a => a.RouteStopId == id)) return Conflict(new { message = "Stop has seat applications; it cannot be deleted" });
        db.RouteStops.Remove(stop);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private Task<DriverRoute?> OwnedRoute(int id) =>
        db.Routes.Include(r => r.Stops).FirstOrDefaultAsync(r => r.Id == id && r.DriverId == DriverId);

    private Task<RouteStop?> OwnedStop(int id) =>
        db.RouteStops.FirstOrDefaultAsync(s => s.Id == id && s.Route.DriverId == DriverId);

    private static void Apply(RouteStop stop, StopRequest req)
    {
        stop.Sequence = req.Sequence;
        stop.School = req.School;
        stop.PickupAddress = req.PickupAddress;
        stop.Latitude = req.Latitude;
        stop.Longitude = req.Longitude;
        stop.ArrivalTime = req.ArrivalTime;
    }

    // --- Seat applications ---

    [HttpGet("applications")]
    public async Task<IActionResult> Applications([FromQuery] ApplicationStatus? status)
    {
        var query = db.Applications.Where(a => a.Route.DriverId == DriverId);
        if (status is not null) query = query.Where(a => a.Status == status);
        var apps = await query
            .Include(a => a.Student).ThenInclude(s => s.Parent)
            .Include(a => a.Route)
            .Include(a => a.Stop)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();
        return Ok(apps.Select(a => new
        {
            a.Id, a.Status, a.AppliedAt, a.RespondedAt,
            student = new { a.Student.Id, a.Student.Name, a.Student.School, a.Student.SchoolAddress },
            parent = new { a.Student.Parent.Id, a.Student.Parent.FullName, a.Student.Parent.PhoneNumber, a.Student.Parent.HomeAddress },
            route = new { a.Route.Id, a.Route.Name },
            stop = Dto.Stop(a.Stop),
        }));
    }

    [HttpPost("applications/{id:int}/accept")]
    public async Task<IActionResult> Accept(int id)
    {
        var a = await PendingApplication(id);
        if (a is null) return NotFound();
        if (a.Status != ApplicationStatus.Pending) return Conflict(new { message = $"Application is already {a.Status}" });

        // shortcut: check-then-write; two accepts in the same instant can overbook by one seat, serialize in a transaction if that bites
        var capacity = await db.Vehicles.Where(v => v.DriverId == DriverId).Select(v => v.Capacity).FirstAsync();
        var accepted = await db.Applications.CountAsync(x => x.Route.DriverId == DriverId && x.Status == ApplicationStatus.Accepted);
        if (accepted >= capacity) return Conflict(new { message = "Vehicle is full" });

        a.Status = ApplicationStatus.Accepted;
        a.RespondedAt = DateTime.UtcNow;
        db.Notify(a.Student.ParentId, $"Seat request for {a.Student.Name} on route {a.Route.Name} was accepted.");
        await db.SaveChangesAsync();
        return Ok(new { a.Id, a.Status, a.RespondedAt });
    }

    [HttpPost("applications/{id:int}/decline")]
    public async Task<IActionResult> Decline(int id)
    {
        var a = await PendingApplication(id);
        if (a is null) return NotFound();
        if (a.Status != ApplicationStatus.Pending) return Conflict(new { message = $"Application is already {a.Status}" });

        a.Status = ApplicationStatus.Declined;
        a.RespondedAt = DateTime.UtcNow;
        db.Notify(a.Student.ParentId, $"Seat request for {a.Student.Name} on route {a.Route.Name} was declined.");
        await db.SaveChangesAsync();
        return Ok(new { a.Id, a.Status, a.RespondedAt });
    }

    private Task<Application?> PendingApplication(int id) =>
        db.Applications.Include(a => a.Student).Include(a => a.Route)
            .FirstOrDefaultAsync(a => a.Id == id && a.Route.DriverId == DriverId);

    // --- Payments ---

    [HttpGet("payments")]
    public async Task<IActionResult> Payments([FromQuery] int? year, [FromQuery] int? month)
    {
        var (y, m) = Period(year, month);
        var items = await db.Applications
            .Where(a => a.Route.DriverId == DriverId && a.Status == ApplicationStatus.Accepted)
            .Select(a => new
            {
                applicationId = a.Id,
                student = a.Student.Name,
                parent = new { a.Student.Parent.Id, a.Student.Parent.FullName, a.Student.Parent.PhoneNumber },
                route = a.Route.Name,
                amountDue = a.Route.PricePerMonth,
                payment = a.Payments
                    .Where(p => p.BillingYear == y && p.BillingMonth == m)
                    .Select(p => new { p.Id, p.Amount, p.Method, p.Status, p.SlipImageUrl, p.PaidAt })
                    .FirstOrDefault(),
            })
            .ToListAsync();
        return Ok(new { year = y, month = m, items });
    }

    [HttpPost("payments")]
    public async Task<IActionResult> LogCashPayment(ManualPaymentRequest req)
    {
        var a = await db.Applications.Include(x => x.Route).Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == req.ApplicationId && x.Route.DriverId == DriverId && x.Status == ApplicationStatus.Accepted);
        if (a is null) return NotFound();

        var p = await db.Payments.FirstOrDefaultAsync(x => x.ApplicationId == a.Id && x.BillingYear == req.BillingYear && x.BillingMonth == req.BillingMonth);
        if (p is null)
        {
            p = new Payment { ApplicationId = a.Id, BillingYear = req.BillingYear, BillingMonth = req.BillingMonth };
            db.Payments.Add(p);
        }
        p.Amount = req.Amount ?? a.Route.PricePerMonth;
        p.Method = PaymentMethod.Cash;
        p.Status = PaymentStatus.Completed;
        p.PaidAt = DateTime.UtcNow;
        db.Notify(a.Student.ParentId, $"Cash payment of LKR {p.Amount} for {a.Student.Name} ({req.BillingYear}-{req.BillingMonth:00}) was recorded.");
        await db.SaveChangesAsync();
        return Ok(Dto.Payment(p));
    }

    [HttpPost("payments/{id:int}/confirm")]
    public async Task<IActionResult> ConfirmPayment(int id)
    {
        var p = await db.Payments.Include(x => x.Application).ThenInclude(a => a.Student)
            .FirstOrDefaultAsync(x => x.Id == id && x.Application.Route.DriverId == DriverId);
        if (p is null) return NotFound();
        if (p.Status == PaymentStatus.Completed) return Conflict(new { message = "Already confirmed" });

        p.Status = PaymentStatus.Completed;
        p.PaidAt = DateTime.UtcNow;
        db.Notify(p.Application.Student.ParentId, $"Your {p.BillingYear}-{p.BillingMonth:00} payment for {p.Application.Student.Name} was confirmed.");
        await db.SaveChangesAsync();
        return Ok(Dto.Payment(p));
    }

    [HttpPost("reminders")]
    public async Task<IActionResult> SendReminders(ReminderRequest req)
    {
        var (y, m) = Period(req.BillingYear, req.BillingMonth);
        var unpaid = await db.Applications
            .Where(a => a.Route.DriverId == DriverId && a.Status == ApplicationStatus.Accepted
                        && !a.Payments.Any(p => p.BillingYear == y && p.BillingMonth == m && p.Status == PaymentStatus.Completed))
            .Select(a => new { a.Student.ParentId, student = a.Student.Name, route = a.Route.Name, amount = a.Route.PricePerMonth })
            .ToListAsync();
        foreach (var u in unpaid)
            db.Notify(u.ParentId, $"Reminder: LKR {u.amount} for {u.student} on route {u.route} is due for {y}-{m:00}.");
        await db.SaveChangesAsync();
        return Ok(new { sent = unpaid.Count });
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> Subscriptions()
    {
        var subs = await db.DriverSubscriptions.Where(s => s.DriverId == DriverId)
            .OrderByDescending(s => s.BillingYear).ThenByDescending(s => s.BillingMonth).ToListAsync();
        return Ok(subs.Select(Dto.Subscription));
    }

    private static (int, int) Period(int? year, int? month) =>
        year is not null && month is not null ? (year.Value, month.Value) : BillingPeriod.Current();
}

public record BankDetailsRequest(string BankName, string BankBranch, string BankAccountNumber);

public record RouteRequest(
    string Name,
    [property: Range(0, 1_000_000)] decimal PricePerDay,
    [property: Range(0, 1_000_000)] decimal PricePerMonth);

public record StopRequest(
    int Sequence,
    string School,
    string PickupAddress,
    [property: Range(-90, 90)] double Latitude,
    [property: Range(-180, 180)] double Longitude,
    TimeOnly ArrivalTime);

public record ManualPaymentRequest(
    int ApplicationId,
    [property: Range(2024, 2100)] int BillingYear,
    [property: Range(1, 12)] int BillingMonth,
    [property: Range(0, 1_000_000)] decimal? Amount);

public record ReminderRequest(
    [property: Range(2024, 2100)] int? BillingYear,
    [property: Range(1, 12)] int? BillingMonth);
