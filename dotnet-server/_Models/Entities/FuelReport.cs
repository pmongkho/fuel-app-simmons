using dotnet_server.Domain.Enums;

namespace dotnet_server.Domain.Entities;

public class FuelReport
{
    public int Id { get; set; }
    public DateOnly ReportDate { get; set; }
    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public FuelReportStatus Status { get; set; } = FuelReportStatus.Draft;
    public decimal TotalRedDiesel { get; set; }
    public decimal TotalClearDiesel { get; set; }
    public decimal TotalDef { get; set; }
    public decimal OverallTotalGallons { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }
    public List<FuelEntry> Entries { get; set; } = [];
}
