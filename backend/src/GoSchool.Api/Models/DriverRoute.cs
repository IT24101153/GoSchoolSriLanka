namespace GoSchool.Api.Models;

public class DriverRoute
{
    public int Id { get; set; }

    public int DriverId { get; set; }
    public User Driver { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public decimal PricePerDay { get; set; }
    public decimal PricePerMonth { get; set; }

    public ICollection<RouteStop> Stops { get; set; } = new List<RouteStop>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
