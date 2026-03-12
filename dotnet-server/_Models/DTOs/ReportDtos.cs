namespace dotnet_server.Application.DTOs;

public class CreateReportRequest
{
    public DateTime ReportDate { get; set; }
    public string? Expectations { get; set; }
    public string? TrailersOnYard { get; set; }
    public string? MechanicalIssues { get; set; }
}

public class CreateFuelEntryRequest
{
    public string TrailerNumber { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public string StartGaugeLevel { get; set; } = string.Empty;
    public string EndGaugeLevel { get; set; } = string.Empty;
    public decimal GallonsPumped { get; set; }
    public string? Notes { get; set; }
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
