namespace GoSchool.Api.Models;

public class Student
{
    public int Id { get; set; }

    public int ParentId { get; set; }
    public User Parent { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public string SchoolAddress { get; set; } = string.Empty;
    public double SchoolLatitude { get; set; }
    public double SchoolLongitude { get; set; }
    public string PrimaryContactNumber { get; set; } = string.Empty;
    public string EmergencyContactNumber { get; set; } = string.Empty;

    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
