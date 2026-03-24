using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using dotnet_server._Data;
using dotnet_server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace dotnet_server.Application.Services;

public sealed class ResendOptions
{
    public const string SectionName = "Resend";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.resend.com";
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Fuel App";
}

internal sealed class ResendEmailAddress
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

internal sealed class ResendSendEmailRequest
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public List<string> To { get; set; } = [];

    [JsonPropertyName("reply_to")]
    public List<ResendEmailAddress>? ReplyTo { get; set; }

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("html")]
    public string Html { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

internal sealed class ResendSendEmailResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class EmailService(
    AppDbContext dbContext,
    HttpClient httpClient,
    IOptions<ResendOptions> resendOptions,
    ILogger<EmailService> logger)
{
    public async Task SendReportSubmittedAsync(FuelReport report, string employeeName)
    {
        var recipients = await dbContext.NotificationRecipients.Where(x => x.IsActive).ToListAsync();
        if (recipients.Count == 0)
        {
            logger.LogInformation("No notification recipients configured.");
            return;
        }

        var options = resendOptions.Value;
        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.FromEmail))
        {
            logger.LogWarning("Resend is not configured. Missing ApiKey or FromEmail.");

            foreach (var recipient in recipients)
            {
                dbContext.EmailLogs.Add(new EmailLog
                {
                    FuelReportId = report.Id,
                    RecipientEmail = recipient.Email,
                    Subject = BuildSubject(report),
                    Status = "Skipped",
                    ErrorMessage = "Resend is not configured.",
                    SentAtUtc = DateTime.UtcNow
                });
            }

            await dbContext.SaveChangesAsync();
            return;
        }

        httpClient.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        foreach (var recipient in recipients)
        {
            var subject = BuildSubject(report);

            try
            {
                var payload = new ResendSendEmailRequest
                {
                    From = BuildFromAddress(options),
                    To = [recipient.Email],
                    Subject = subject,
                    Text = BuildTextBody(report, employeeName),
                    Html = BuildHtmlBody(report, employeeName)
                };

                var response = await httpClient.PostAsJsonAsync("emails", payload);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var resendResponse = string.IsNullOrWhiteSpace(responseBody)
                        ? null
                        : System.Text.Json.JsonSerializer.Deserialize<ResendSendEmailResponse>(responseBody);

                    dbContext.EmailLogs.Add(new EmailLog
                    {
                        FuelReportId = report.Id,
                        RecipientEmail = recipient.Email,
                        Subject = subject,
                        Status = "Sent",
                        ProviderMessageId = resendResponse?.Id,
                        SentAtUtc = DateTime.UtcNow
                    });
                }
                else
                {
                    logger.LogError(
                        "Resend email send failed for report {ReportId} to {RecipientEmail}. Status {StatusCode}. Body: {ResponseBody}",
                        report.Id,
                        recipient.Email,
                        (int)response.StatusCode,
                        responseBody);

                    dbContext.EmailLogs.Add(new EmailLog
                    {
                        FuelReportId = report.Id,
                        RecipientEmail = recipient.Email,
                        Subject = subject,
                        Status = "Failed",
                        ErrorMessage = $"Resend API returned {(int)response.StatusCode}: {responseBody}",
                        SentAtUtc = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error sending report notification for report {ReportId} to {RecipientEmail}.", report.Id, recipient.Email);

                dbContext.EmailLogs.Add(new EmailLog
                {
                    FuelReportId = report.Id,
                    RecipientEmail = recipient.Email,
                    Subject = subject,
                    Status = "Failed",
                    ErrorMessage = ex.Message,
                    SentAtUtc = DateTime.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static string BuildSubject(FuelReport report) => $"Fuel report submitted: {report.ReportDate:yyyy-MM-dd}";

    private static string BuildFromAddress(ResendOptions options) =>
        string.IsNullOrWhiteSpace(options.FromName)
            ? options.FromEmail.Trim()
            : $"{options.FromName.Trim()} <{options.FromEmail.Trim()}>";

    private static string BuildTextBody(FuelReport report, string employeeName) =>
        $"""
         A fuel report has been submitted.

         Employee: {employeeName}
         Report date: {report.ReportDate:yyyy-MM-dd}
         Report ID: {report.Id}
         Submitted at (UTC): {report.SubmittedAtUtc:yyyy-MM-dd HH:mm:ss}

         Please review it in Fuel App.
         """;

    private static string BuildHtmlBody(FuelReport report, string employeeName) =>
        $"""
         <p>A fuel report has been submitted.</p>
         <ul>
           <li><strong>Employee:</strong> {System.Net.WebUtility.HtmlEncode(employeeName)}</li>
           <li><strong>Report date:</strong> {report.ReportDate:yyyy-MM-dd}</li>
           <li><strong>Report ID:</strong> {report.Id}</li>
           <li><strong>Submitted at (UTC):</strong> {report.SubmittedAtUtc:yyyy-MM-dd HH:mm:ss}</li>
         </ul>
         <p>Please review it in Fuel App.</p>
         """;
}
