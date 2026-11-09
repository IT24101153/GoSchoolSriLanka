using GoSchool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GoSchool.Api.Data;

public class GoSchoolDbContext(DbContextOptions<GoSchoolDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<DriverRoute> Routes => Set<DriverRoute>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
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

        modelBuilder.Entity<Application>()
            .HasOne(a => a.Student)
            .WithMany(s => s.Applications)
            .HasForeignKey(a => a.StudentId);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.Route)
            .WithMany(r => r.Applications)
            .HasForeignKey(a => a.RouteId);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Application)
            .WithMany(a => a.Payments)
            .HasForeignKey(p => p.ApplicationId);

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
