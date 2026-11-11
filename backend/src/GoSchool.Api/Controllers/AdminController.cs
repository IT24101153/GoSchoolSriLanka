using System.ComponentModel.DataAnnotations;
using GoSchool.Api.Data;
using GoSchool.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoSchool.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(GoSchoolDbContext db) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var (year, month) = BillingPeriod.Current();
        var thisMonth = db.DriverSubscriptions.Where(s => s.BillingYear == year && s.BillingMonth == month);
        return Ok(new
        {
            activeDrivers = await db.Users.CountAsync(u => u.Role == UserRole.Driver && u.Status == AccountStatus.Active),
            pendingDrivers = await db.Users.CountAsync(u => u.Role == UserRole.Driver && u.Status == AccountStatus.Pending),
            registeredParents = await db.Users.CountAsync(u => u.Role == UserRole.Parent),
            registeredStudents = await db.Students.CountAsync(),
            totalVehicles = await db.Vehicles.CountAsync(),
            subscriptions = new
            {
                year,
                month,
                paid = await thisMonth.CountAsync(s => s.Status == PaymentStatus.Completed),
                pending = await thisMonth.CountAsync(s => s.Status == PaymentStatus.Pending),
                overdue = await db.DriverSubscriptions.CountAsync(s => s.Status == PaymentStatus.Pending
                    && (s.BillingYear < year || (s.BillingYear == year && s.BillingMonth < month))),
                revenue = await thisMonth.Where(s => s.Status == PaymentStatus.Completed).SumAsync(s => s.Amount),
            },
        });
    }

    [HttpGet("drivers")]
    public async Task<IActionResult> Drivers([FromQuery] AccountStatus? status)
    {
        var query = db.Users.Where(u => u.Role == UserRole.Driver);
        if (status is not null) query = query.Where(u => u.Status == status);
        var drivers = await query.Include(u => u.Vehicle).OrderByDescending(u => u.CreatedAt).ToListAsync();
        return Ok(drivers.Select(u => new
        {
            u.Id, u.FullName, u.Email, u.PhoneNumber, u.Status, u.CreatedAt,
            vehicle = u.Vehicle is null ? null : Dto.Vehicle(u.Vehicle),
        }));
    }

    [HttpGet("drivers/{id:int}")]
    public async Task<IActionResult> Driver(int id)
    {
        var u = await db.Users
            .Include(x => x.Vehicle)
            .Include(x => x.Routes).ThenInclude(r => r.Stops)
            .Include(x => x.Subscriptions)
            .FirstOrDefaultAsync(x => x.Id == id && x.Role == UserRole.Driver);
        if (u is null) return NotFound();

        return Ok(new
        {
            u.Id, u.FullName, u.Email, u.PhoneNumber, u.Status, u.CreatedAt,
            bank = new { u.BankName, u.BankBranch, u.BankAccountNumber },
            vehicle = u.Vehicle is null ? null : Dto.Vehicle(u.Vehicle),
            routes = u.Routes.Select(Dto.Route),
            subscriptions = u.Subscriptions.OrderByDescending(s => s.BillingYear).ThenByDescending(s => s.BillingMonth).Select(Dto.Subscription),
            acceptedSeats = await db.Applications.CountAsync(a => a.Route.DriverId == id && a.Status == ApplicationStatus.Accepted),
        });
    }

    [HttpPost("drivers/{id:int}/approve")]
    public Task<IActionResult> Approve(int id) => Review(id, AccountStatus.Active, "Your GoSchool driver account has been approved. You can now set up your routes.");

    [HttpPost("drivers/{id:int}/reject")]
    public Task<IActionResult> Reject(int id) => Review(id, AccountStatus.Rejected, "Your GoSchool driver registration was rejected.");

    private async Task<IActionResult> Review(int id, AccountStatus decision, string message)
    {
        var u = await db.Users.Include(x => x.Vehicle).FirstOrDefaultAsync(x => x.Id == id && x.Role == UserRole.Driver);
        if (u is null) return NotFound();
        if (u.Status != AccountStatus.Pending) return Conflict(new { message = $"Driver is already {u.Status}" });

        u.Status = decision;
        if (u.Vehicle is not null) u.Vehicle.ApprovalStatus = decision;
        db.Notify(u.Id, message);
        await db.SaveChangesAsync();
        return Ok(Dto.User(u));
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] UserRole? role)
    {
        var query = db.Users.Where(u => u.Role != UserRole.Admin);
        if (role is not null) query = query.Where(u => u.Role == role);
        var users = await query.OrderBy(u => u.FullName).ToListAsync();
        return Ok(users.Select(Dto.User));
    }

    [HttpPost("users/{id:int}/suspend")]
    public Task<IActionResult> Suspend(int id) => SetStatus(id, AccountStatus.Suspended);

    [HttpPost("users/{id:int}/reactivate")]
    public Task<IActionResult> Reactivate(int id) => SetStatus(id, AccountStatus.Active);

    private async Task<IActionResult> SetStatus(int id, AccountStatus status)
    {
        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == id && x.Role != UserRole.Admin);
        if (u is null) return NotFound();
        if (u.Status is AccountStatus.Pending or AccountStatus.Rejected)
            return Conflict(new { message = $"Use the approval workflow for a {u.Status} driver" });

        u.Status = status;
        if (status == AccountStatus.Suspended) u.ApiToken = null;
        await db.SaveChangesAsync();
        return Ok(Dto.User(u));
    }

    [HttpGet("vehicles")]
    public async Task<IActionResult> Vehicles()
    {
        var vehicles = await db.Vehicles.Include(v => v.Driver).OrderBy(v => v.RegistrationNumber).ToListAsync();
        return Ok(vehicles.Select(v => new
        {
            v.Id, v.RegistrationNumber, v.Capacity, v.PhotoUrls, v.ApprovalStatus,
            driver = Dto.User(v.Driver),
        }));
    }

    [HttpGet("students")]
    public async Task<IActionResult> Students()
    {
        var students = await db.Students
            .Include(s => s.Parent)
            .Include(s => s.Applications.Where(a => a.Status == ApplicationStatus.Accepted))
                .ThenInclude(a => a.Route).ThenInclude(r => r.Driver)
            .Include(s => s.Applications.Where(a => a.Status == ApplicationStatus.Accepted))
                .ThenInclude(a => a.Stop)
            .OrderBy(s => s.Name)
            .ToListAsync();
        return Ok(students.Select(s => new
        {
            s.Id, s.Name, s.School, s.SchoolAddress, s.PrimaryContactNumber, s.EmergencyContactNumber,
            parent = Dto.User(s.Parent),
            assignment = s.Applications.Select(a => new
            {
                routeId = a.RouteId,
                route = a.Route.Name,
                driver = a.Route.Driver.FullName,
                stop = a.Stop.School,
                arrivalTime = a.Stop.ArrivalTime,
            }).FirstOrDefault(),
        }));
    }

    [HttpGet("routes")]
    public async Task<IActionResult> Routes()
    {
        var routes = await db.Routes.Include(r => r.Driver).Include(r => r.Stops).OrderBy(r => r.Driver.FullName).ToListAsync();
        return Ok(routes.Select(r => new { driver = Dto.User(r.Driver), route = Dto.Route(r) }));
    }

    [HttpGet("payments")]
    public async Task<IActionResult> Payments([FromQuery] int? year, [FromQuery] int? month)
    {
        var query = db.Payments.AsQueryable();
        if (year is not null) query = query.Where(p => p.BillingYear == year);
        if (month is not null) query = query.Where(p => p.BillingMonth == month);
        var payments = await query
            .Include(p => p.Application).ThenInclude(a => a.Student).ThenInclude(s => s.Parent)
            .Include(p => p.Application).ThenInclude(a => a.Route).ThenInclude(r => r.Driver)
            .OrderByDescending(p => p.BillingYear).ThenByDescending(p => p.BillingMonth)
            .ToListAsync();
        return Ok(payments.Select(p => new
        {
            payment = Dto.Payment(p),
            student = p.Application.Student.Name,
            parent = p.Application.Student.Parent.FullName,
            driver = p.Application.Route.Driver.FullName,
            route = p.Application.Route.Name,
        }));
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> Subscriptions([FromQuery] int? year, [FromQuery] int? month)
    {
        var query = db.DriverSubscriptions.AsQueryable();
        if (year is not null) query = query.Where(s => s.BillingYear == year);
        if (month is not null) query = query.Where(s => s.BillingMonth == month);
        var subs = await query.Include(s => s.Driver)
            .OrderByDescending(s => s.BillingYear).ThenByDescending(s => s.BillingMonth).ThenBy(s => s.Driver.FullName)
            .ToListAsync();
        return Ok(subs.Select(s => new { subscription = Dto.Subscription(s), driver = Dto.User(s.Driver) }));
    }

    [HttpPost("subscriptions/generate")]
    public async Task<IActionResult> GenerateSubscriptions(GenerateSubscriptionsRequest req)
    {
        var billed = db.DriverSubscriptions.Where(s => s.BillingYear == req.Year && s.BillingMonth == req.Month).Select(s => s.DriverId);
        var drivers = await db.Users
            .Where(u => u.Role == UserRole.Driver && u.Status == AccountStatus.Active && !billed.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();
        foreach (var driverId in drivers)
        {
            db.DriverSubscriptions.Add(new DriverSubscription { DriverId = driverId, BillingYear = req.Year, BillingMonth = req.Month, Amount = req.Amount });
            db.Notify(driverId, $"Your GoSchool subscription of LKR {req.Amount} for {req.Year}-{req.Month:00} is due.");
        }
        await db.SaveChangesAsync();
        return Ok(new { created = drivers.Count });
    }

    [HttpPost("subscriptions/{id:int}/mark-paid")]
    public async Task<IActionResult> MarkSubscriptionPaid(int id)
    {
        var s = await db.DriverSubscriptions.FindAsync(id);
        if (s is null) return NotFound();
        if (s.Status == PaymentStatus.Completed) return Conflict(new { message = "Already paid" });

        s.Status = PaymentStatus.Completed;
        s.PaidAt = DateTime.UtcNow;
        db.Notify(s.DriverId, $"Your GoSchool subscription for {s.BillingYear}-{s.BillingMonth:00} is marked as paid.");
        await db.SaveChangesAsync();
        return Ok(Dto.Subscription(s));
    }
}

public record GenerateSubscriptionsRequest(
    [property: Range(2024, 2100)] int Year,
    [property: Range(1, 12)] int Month,
    [property: Range(0, 1_000_000)] decimal Amount);
