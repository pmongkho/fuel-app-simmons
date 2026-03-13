namespace dotnet_server.Domain.Entities;

public class Trailer
{
    public int Id { get; set; }
    public string TrailerNumber { get; set; } = string.Empty;
    public string Location { get; set; } = "Main";
    public bool IsTankFull { get; set; }
    public bool HasMechanicalIssues { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<FuelEntry> FuelEntries { get; set; } = [];
}
