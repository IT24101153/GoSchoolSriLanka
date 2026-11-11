using System.ComponentModel.DataAnnotations;
using GoSchool.Api.Data;
using GoSchool.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoSchool.Api.Controllers;

[ApiController]
[Route("api/parent")]
[Authorize(Roles = "Parent")]
public class ParentController(GoSchoolDbContext db) : ControllerBase
{
    private int ParentId => User.UserId();

    // --- Students ---

    [HttpGet("students")]
    public async Task<IActionResult> Students()
    {
        var students = await db.Students.Where(s => s.ParentId == ParentId).OrderBy(s => s.Name).ToListAsync();
        return Ok(students.Select(StudentDto));
    }

    [HttpPost("students")]
    public async Task<IActionResult> AddStudent(StudentRequest req)
    {
        var s = new Student { ParentId = ParentId };
        Apply(s, req);
        db.Students.Add(s);
        await db.SaveChangesAsync();
        return Ok(StudentDto(s));
    }

    [HttpPut("students/{id:int}")]
    public async Task<IActionResult> UpdateStudent(int id, StudentRequest req)
    {
        var s = await OwnedStudent(id);
        if (s is null) return NotFound();
        Apply(s, req);
        await db.SaveChangesAsync();
        return Ok(StudentDto(s));
    }

    [HttpDelete("students/{id:int}")]
    public async Task<IActionResult> DeleteStudent(int id)
    {
        var s = await OwnedStudent(id);
        if (s is null) return NotFound();
        if (await db.Applications.AnyAsync(a => a.StudentId == id)) return Conflict(new { message = "Student has seat applications; it cannot be deleted" });
        db.Students.Remove(s);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private Task<Student?> OwnedStudent(int id) => db.Students.FirstOrDefaultAsync(s => s.Id == id && s.ParentId == ParentId);

    private static void Apply(Student s, StudentRequest req)
    {
        s.Name = req.Name;
        s.School = req.School;
        s.SchoolAddress = req.SchoolAddress;
        s.SchoolLatitude = req.SchoolLatitude;
        s.SchoolLongitude = req.SchoolLongitude;
        s.PrimaryContactNumber = req.PrimaryContactNumber;
        s.EmergencyContactNumber = req.EmergencyContactNumber;
    }

    private static object StudentDto(Student s) => new
    {
        s.Id, s.Name, s.School, s.SchoolAddress, s.SchoolLatitude, s.SchoolLongitude, s.PrimaryContactNumber, s.EmergencyContactNumber,
    };

    // --- Driver discovery ---

    [HttpGet("drivers")]
    public async Task<IActionResult> Drivers([FromQuery] string? school, [FromQuery] double radiusKm = 10)
    {
        var cards = await RouteCards(r => true);
        var filtered = cards
            .Where(c => c.DistanceKm <= radiusKm)
            .Where(c => school is null || c.Route.Stops.Any(s => s.School.Contains(school, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(c => c.DistanceKm);
        return Ok(filtered.Select(Card));
    }

    [HttpGet("drivers/{driverId:int}")]
    public async Task<IActionResult> Driver(int driverId)
    {
        var cards = await RouteCards(r => r.DriverId == driverId);
        if (cards.Count == 0) return NotFound();
        return Ok(cards.OrderBy(c => c.DistanceKm).Select(Card));
    }

    private record RouteCard(DriverRoute Route, double DistanceKm, int SeatsAvailable);

    // shortcut: distance is computed in memory over every active route; add a bounding-box prefilter or PostGIS past a few thousand routes
    private async Task<List<RouteCard>> RouteCards(System.Linq.Expressions.Expression<Func<DriverRoute, bool>> filter)
    {
        var me = await db.Users.AsNoTracking().FirstAsync(u => u.Id == ParentId);
        var routes = await db.Routes
            .Where(r => r.Driver.Status == AccountStatus.Active && r.Driver.Vehicle!.ApprovalStatus == AccountStatus.Active && r.Stops.Any())
            .Where(filter)
            .Include(r => r.Driver).ThenInclude(d => d.Vehicle)
            .Include(r => r.Stops)
            .ToListAsync();
        var acceptedByDriver = await db.Applications
            .Where(a => a.Status == ApplicationStatus.Accepted)
            .GroupBy(a => a.Route.DriverId)
            .Select(g => new { DriverId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DriverId, x => x.Count);

        return routes.Select(r => new RouteCard(
            r,
            r.Stops.Min(s => Geo.DistanceKm(me.HomeLatitude!.Value, me.HomeLongitude!.Value, s.Latitude, s.Longitude)),
            r.Driver.Vehicle!.Capacity - acceptedByDriver.GetValueOrDefault(r.DriverId))).ToList();
    }

    private static object Card(RouteCard c) => new
    {
        driver = new { c.Route.Driver.Id, c.Route.Driver.FullName, c.Route.Driver.PhoneNumber },
        vehicle = Dto.Vehicle(c.Route.Driver.Vehicle!),
        seatsAvailable = c.SeatsAvailable,
        route = Dto.Route(c.Route),
        distanceKm = Math.Round(c.DistanceKm, 1),
    };

    // --- Seat applications ---

    [HttpPost("applications")]
    public async Task<IActionResult> Apply(ApplyRequest req)
    {
        var student = await OwnedStudent(req.StudentId);
        if (student is null) return NotFound(new { message = "Student not found" });

        var stop = await db.RouteStops.Include(s => s.Route).ThenInclude(r => r.Driver)
            .FirstOrDefaultAsync(s => s.Id == req.RouteStopId && s.RouteId == req.RouteId);
        if (stop is null) return NotFound(new { message = "Stop not found on that route" });
        if (stop.Route.Driver.Status != AccountStatus.Active) return Conflict(new { message = "Driver is not active" });

        if (await db.Applications.AnyAsync(a => a.StudentId == student.Id && a.Status != ApplicationStatus.Declined))
            return Conflict(new { message = "Student already has a pending or accepted seat" });

        var a = new Application { StudentId = student.Id, RouteId = req.RouteId, RouteStopId = req.RouteStopId };
        db.Applications.Add(a);
        db.Notify(stop.Route.DriverId, $"New seat request for {student.Name} at {stop.School} on route {stop.Route.Name}.");
        await db.SaveChangesAsync();
        return Ok(new { a.Id, a.Status, a.AppliedAt });
    }

    [HttpGet("applications")]
    public async Task<IActionResult> Applications()
    {
        var apps = await db.Applications
            .Where(a => a.Student.ParentId == ParentId)
            .Include(a => a.Student)
            .Include(a => a.Route).ThenInclude(r => r.Driver)
            .Include(a => a.Stop)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();
        return Ok(apps.Select(a => new
        {
            a.Id, a.Status, a.AppliedAt, a.RespondedAt,
            student = new { a.Student.Id, a.Student.Name },
            route = new { a.Route.Id, a.Route.Name, a.Route.PricePerDay, a.Route.PricePerMonth },
            stop = Dto.Stop(a.Stop),
            driver = new
            {
                a.Route.Driver.Id, a.Route.Driver.FullName, a.Route.Driver.PhoneNumber,
                bank = a.Status == ApplicationStatus.Accepted
                    ? new { a.Route.Driver.BankName, a.Route.Driver.BankBranch, a.Route.Driver.BankAccountNumber }
                    : null,
            },
        }));
    }

    // --- Payments ---

    [HttpGet("payments")]
    public async Task<IActionResult> Payments()
    {
        var payments = await db.Payments
            .Where(p => p.Application.Student.ParentId == ParentId)
            .Include(p => p.Application).ThenInclude(a => a.Student)
            .Include(p => p.Application).ThenInclude(a => a.Route).ThenInclude(r => r.Driver)
            .OrderByDescending(p => p.BillingYear).ThenByDescending(p => p.BillingMonth)
            .ToListAsync();
        return Ok(payments.Select(p => new
        {
            payment = Dto.Payment(p),
            student = p.Application.Student.Name,
            driver = p.Application.Route.Driver.FullName,
            route = p.Application.Route.Name,
        }));
    }

    [HttpPost("payments")]
    public async Task<IActionResult> SubmitPayment(ParentPaymentRequest req)
    {
        var a = await db.Applications.Include(x => x.Route).Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == req.ApplicationId && x.Student.ParentId == ParentId && x.Status == ApplicationStatus.Accepted);
        if (a is null) return NotFound(new { message = "Accepted application not found" });
        if (req.Method == PaymentMethod.Bank && string.IsNullOrWhiteSpace(req.SlipImageUrl))
            return BadRequest(new { message = "Upload the bank transfer slip first" });

        var p = await db.Payments.FirstOrDefaultAsync(x => x.ApplicationId == a.Id && x.BillingYear == req.BillingYear && x.BillingMonth == req.BillingMonth);
        if (p?.Status == PaymentStatus.Completed) return Conflict(new { message = "This month is already paid" });
        if (p is null)
        {
            p = new Payment { ApplicationId = a.Id, BillingYear = req.BillingYear, BillingMonth = req.BillingMonth };
            db.Payments.Add(p);
        }
        p.Amount = a.Route.PricePerMonth;
        p.Method = req.Method;
        p.SlipImageUrl = req.SlipImageUrl;
        p.Status = PaymentStatus.Pending;
        db.Notify(a.Route.DriverId, $"{a.Student.Parent?.FullName ?? "A parent"} marked {req.BillingYear}-{req.BillingMonth:00} for {a.Student.Name} as paid via {req.Method}. Please confirm.");
        await db.SaveChangesAsync();
        return Ok(Dto.Payment(p));
    }
}

public record StudentRequest(
    string Name,
    string School,
    string SchoolAddress,
    [property: Range(-90, 90)] double SchoolLatitude,
    [property: Range(-180, 180)] double SchoolLongitude,
    string PrimaryContactNumber,
    string EmergencyContactNumber);

public record ApplyRequest(int StudentId, int RouteId, int RouteStopId);

public record ParentPaymentRequest(
    int ApplicationId,
    [property: Range(2024, 2100)] int BillingYear,
    [property: Range(1, 12)] int BillingMonth,
    PaymentMethod Method,
    string? SlipImageUrl);
