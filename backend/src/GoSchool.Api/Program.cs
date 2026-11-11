using System.Text.Json.Serialization;
using GoSchool.Api;
using GoSchool.Api.Data;
using GoSchool.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services.AddDbContext<GoSchoolDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddAuthentication(TokenAuthHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, TokenAuthHandler>(TokenAuthHandler.SchemeName, _ => { });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ActiveDriver", p => p.RequireRole("Driver").RequireClaim("status", "Active"));

const string AdminWebOrigin = "AdminWebOrigin";
builder.Services.AddCors(options =>
{
    options.AddPolicy(AdminWebOrigin, policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// shortcut: migrates and seeds the admin on startup; move to a deploy step before running more than one instance
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GoSchoolDbContext>();
    await db.Database.MigrateAsync();

    if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var admin = new User
        {
            Role = UserRole.Admin,
            FullName = "Administrator",
            Email = app.Configuration["Admin:Email"]!,
        };
        admin.PasswordHash = hasher.HashPassword(admin, app.Configuration["Admin:Password"]!);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(AdminWebOrigin);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
