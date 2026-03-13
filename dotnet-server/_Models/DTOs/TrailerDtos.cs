namespace dotnet_server.Application.DTOs;

public class CreateTrailerRequest
{
    public string TrailerNumber { get; set; } = string.Empty;
    public string Location { get; set; } = "Main";
    public bool IsTankFull { get; set; }
    public bool HasMechanicalIssues { get; set; }
    public string? Notes { get; set; }
}

public class UpdateTrailerRequest
{
    public string Location { get; set; } = "Main";
    public bool IsTankFull { get; set; }
    public bool HasMechanicalIssues { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
