using dotnet_server._Data;
using dotnet_server.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server.Application.Services;

public class EmailService(AppDbContext dbContext, ILogger<EmailService> logger)
{
    public async Task SendReportSubmittedAsync(FuelReport report, string employeeName)
    {
        var recipients = await dbContext.NotificationRecipients.Where(x => x.IsActive).ToListAsync();
        if (recipients.Count == 0)
        {
            logger.LogInformation("No notification recipients configured.");
            return;
        }

        foreach (var recipient in recipients)
        {
            dbContext.EmailLogs.Add(new EmailLog
            {
                FuelReportId = report.Id,
                RecipientEmail = recipient.Email,
                Subject = $"Fuel report submitted: {report.ReportDate}",
                Status = "Sent",
                ProviderMessageId = "dev-local",
                SentAtUtc = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
    }
}
