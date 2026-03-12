namespace dotnet_server.Domain.Entities;

public class EmailLog
{
    public int Id { get; set; }
    public int? FuelReportId { get; set; }
    public int? FuelEntryId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
