using dotnet_server.Domain.Enums;

namespace dotnet_server.Domain.Entities;

public class FuelReport
{
    public int Id { get; set; }
    public DateOnly ReportDate { get; set; }
    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public FuelReportStatus Status { get; set; } = FuelReportStatus.Draft;
    public int FuelingTankLevelStart { get; set; }
    public int FuelingTankLevelEnd { get; set; }
    public decimal TotalRedDiesel { get; set; }
    public decimal TotalClearDiesel { get; set; }
    public decimal TotalDef { get; set; }
    public decimal OverallTotalGallons { get; set; }
    public int? StartGaugeSignedBySupervisorId { get; set; }
    public User? StartGaugeSignedBySupervisor { get; set; }
    public DateTime? StartGaugeSignedAtUtc { get; set; }
    public string? StartGaugeSupervisorSignatureName { get; set; }
    public int? EndGaugeSignedBySupervisorId { get; set; }
    public User? EndGaugeSignedBySupervisor { get; set; }
    public DateTime? EndGaugeSignedAtUtc { get; set; }
    public string? EndGaugeSupervisorSignatureName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }
    public List<FuelEntry> Entries { get; set; } = [];
}
