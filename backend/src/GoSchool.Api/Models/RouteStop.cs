namespace GoSchool.Api.Models;

public class RouteStop
{
    public int Id { get; set; }

    public int RouteId { get; set; }
    public DriverRoute Route { get; set; } = null!;

    public int Sequence { get; set; }
    public string School { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public TimeOnly ArrivalTime { get; set; }
}
