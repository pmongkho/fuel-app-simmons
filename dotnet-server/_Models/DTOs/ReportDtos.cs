namespace dotnet_server.Application.DTOs;

public class CreateReportRequest
{
    public DateTime ReportDate { get; set; }
    public string? ReportLocation { get; set; }
}

public class CreateFuelEntryRequest
{
    public int TrailerId { get; set; }
    public bool IsTankFull { get; set; }
    public bool HasMechanicalIssues { get; set; }
    public string? TrailerNotes { get; set; }
    public string FuelType { get; set; } = string.Empty;
    public int? FuelingTankLevelStart { get; set; }
    public int? FuelingTankLevelEnd { get; set; }
    public decimal GallonsPumped { get; set; }
}

public class ApproveEntryRequest
{
    public string SignatureName { get; set; } = string.Empty;
}

public class RejectEntryRequest
{
    public string RejectionReason { get; set; } = string.Empty;
}

public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Employee";
}

public class UpdateUserRequest
{
    public string Role { get; set; } = "Employee";
    public bool IsActive { get; set; }
}
