using System.Security.Claims;
using System.Text.Encodings.Web;
using GoSchool.Api.Data;
using GoSchool.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GoSchool.Api;

public class TokenAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    GoSchoolDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Token";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();

        var token = header["Bearer ".Length..].Trim();
        if (token.Length == 0) return AuthenticateResult.NoResult();

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.ApiToken == token);
        if (user is null) return AuthenticateResult.Fail("Invalid token");
        if (user.Status is AccountStatus.Suspended or AccountStatus.Rejected) return AuthenticateResult.Fail("Account disabled");

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("status", user.Status.ToString()),
        ], SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
