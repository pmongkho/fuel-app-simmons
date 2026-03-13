using dotnet_server.Domain.Enums;

namespace dotnet_server.Domain.Entities;

public class FuelEntry
{
    public int Id { get; set; }
    public int FuelReportId { get; set; }
    public FuelReport? FuelReport { get; set; }
    public string TrailerNumber { get; set; } = string.Empty;
    public FuelType FuelType { get; set; }
    public string StartGaugeLevel { get; set; } = string.Empty;
    public string EndGaugeLevel { get; set; } = string.Empty;
    public string TrailerLocation { get; set; } = "Main";
    public int? FuelingTankLevelStart { get; set; }
    public int? FuelingTankLevelEnd { get; set; }
    public decimal GallonsPumped { get; set; }
    public bool HasMechanicalIssues { get; set; }
    public string? Notes { get; set; }
    public int EnteredByUserId { get; set; }
    public User? EnteredByUser { get; set; }
    public DateTime EnteredAtUtc { get; set; } = DateTime.UtcNow;
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public int? VerifiedBySupervisorId { get; set; }
    public User? VerifiedBySupervisor { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string? SupervisorSignatureName { get; set; }
    public string? RejectionReason { get; set; }
    public List<FuelEntryPhoto> Photos { get; set; } = [];
}
