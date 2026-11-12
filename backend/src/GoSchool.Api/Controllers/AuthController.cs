using System.ComponentModel.DataAnnotations;
using GoSchool.Api.Data;
using GoSchool.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoSchool.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(GoSchoolDbContext db, IPasswordHasher<User> hasher) : ControllerBase
{
    [HttpPost("register/driver")]
    public async Task<IActionResult> RegisterDriver(RegisterDriverRequest req)
    {
        var email = Normalize(req.Email);
        if (await db.Users.AnyAsync(u => u.Email == email)) return Conflict(new { message = "Email already registered" });

        var user = new User
        {
            Role = UserRole.Driver,
            Status = AccountStatus.Pending,
            FullName = req.FullName,
            Email = email,
            PhoneNumber = req.PhoneNumber,
            BankName = req.BankName,
            BankBranch = req.BankBranch,
            BankAccountNumber = req.BankAccountNumber,
            ApiToken = Tokens.New(),
            Vehicle = new Vehicle
            {
                RegistrationNumber = req.RegistrationNumber,
                Capacity = req.Capacity,
                PhotoUrls = req.PhotoUrls ?? [],
            },
        };
        user.PasswordHash = hasher.HashPassword(user, req.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Ok(Session(user));
    }

    [HttpPost("register/parent")]
    public async Task<IActionResult> RegisterParent(RegisterParentRequest req)
    {
        var email = Normalize(req.Email);
        if (await db.Users.AnyAsync(u => u.Email == email)) return Conflict(new { message = "Email already registered" });

        var user = new User
        {
            Role = UserRole.Parent,
            FullName = req.FullName,
            Email = email,
            PhoneNumber = req.PhoneNumber,
            HomeAddress = req.HomeAddress,
            HomeLatitude = req.HomeLatitude,
            HomeLongitude = req.HomeLongitude,
            ApiToken = Tokens.New(),
        };
        user.PasswordHash = hasher.HashPassword(user, req.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Ok(Session(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var email = Normalize(req.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid email or password" });
        if (user.Status is AccountStatus.Suspended or AccountStatus.Rejected)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = $"Account is {user.Status}" });

        user.ApiToken = Tokens.New();
        await db.SaveChangesAsync();
        return Ok(Session(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await db.Users.Include(u => u.Vehicle).FirstAsync(u => u.Id == User.UserId());
        return Ok(new { user = Dto.User(user), vehicle = user.Vehicle is null ? null : Dto.Vehicle(user.Vehicle) });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var user = await db.Users.FirstAsync(u => u.Id == User.UserId());
        user.ApiToken = null;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Drivers register before they have a token, so vehicle photos are attached right after sign-up.
    [Authorize(Roles = "Driver")]
    [HttpPut("vehicle-photos")]
    public async Task<IActionResult> SetVehiclePhotos(VehiclePhotosRequest req)
    {
        var vehicle = await db.Vehicles.FirstAsync(v => v.DriverId == User.UserId());
        vehicle.PhotoUrls = req.PhotoUrls;
        await db.SaveChangesAsync();
        return Ok(Dto.Vehicle(vehicle));
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static object Session(User user) => new { token = user.ApiToken, user = Dto.User(user) };
}

public record RegisterDriverRequest(
    string FullName,
    [property: EmailAddress] string Email,
    [property: MinLength(6)] string Password,
    string PhoneNumber,
    string RegistrationNumber,
    [property: Range(1, 100)] int Capacity,
    List<string>? PhotoUrls,
    string? BankName,
    string? BankBranch,
    string? BankAccountNumber);

public record RegisterParentRequest(
    string FullName,
    [property: EmailAddress] string Email,
    [property: MinLength(6)] string Password,
    string PhoneNumber,
    string HomeAddress,
    [property: Range(-90, 90)] double HomeLatitude,
    [property: Range(-180, 180)] double HomeLongitude);

public record LoginRequest(string Email, string Password);

public record VehiclePhotosRequest(List<string> PhotoUrls);
