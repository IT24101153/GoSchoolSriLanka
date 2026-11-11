using GoSchool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GoSchool.Api.Data;

public class GoSchoolDbContext(DbContextOptions<GoSchoolDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<DriverRoute> Routes => Set<DriverRoute>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<DriverSubscription> DriverSubscriptions => Set<DriverSubscription>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.ApiToken)
            .IsUnique();

        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Driver)
            .WithOne(u => u.Vehicle)
            .HasForeignKey<Vehicle>(v => v.DriverId);

        modelBuilder.Entity<Student>()
            .HasOne(s => s.Parent)
            .WithMany(u => u.Students)
            .HasForeignKey(s => s.ParentId);

        modelBuilder.Entity<DriverRoute>()
            .HasOne(r => r.Driver)
            .WithMany(u => u.Routes)
            .HasForeignKey(r => r.DriverId);

        modelBuilder.Entity<RouteStop>()
            .HasOne(s => s.Route)
            .WithMany(r => r.Stops)
            .HasForeignKey(s => s.RouteId);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.Student)
            .WithMany(s => s.Applications)
            .HasForeignKey(a => a.StudentId);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.Route)
            .WithMany(r => r.Applications)
            .HasForeignKey(a => a.RouteId);

        // A stop with seats on it cannot be deleted; controllers answer 409 before this is ever hit.
        modelBuilder.Entity<Application>()
            .HasOne(a => a.Stop)
            .WithMany()
            .HasForeignKey(a => a.RouteStopId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Application)
            .WithMany(a => a.Payments)
            .HasForeignKey(p => p.ApplicationId);

        modelBuilder.Entity<Payment>()
            .HasIndex(p => new { p.ApplicationId, p.BillingYear, p.BillingMonth })
            .IsUnique();

        modelBuilder.Entity<DriverSubscription>()
            .HasOne(s => s.Driver)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(s => s.DriverId);

        modelBuilder.Entity<DriverSubscription>()
            .HasIndex(s => new { s.DriverId, s.BillingYear, s.BillingMonth })
            .IsUnique();

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    property.SetColumnType("decimal(10,2)");
                }
            }
        }
    }
}
