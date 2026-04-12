using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json.Serialization;
using dotnet_server._Data;
using dotnet_server.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace dotnet_server.Application.Services;

public sealed class ResendOptions
{
    public const string SectionName = "Resend";
    public const string DefaultFromEmail = "onboarding@resend.dev";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.resend.com";
    public string FromEmail { get; set; } = DefaultFromEmail;
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
    IWebHostEnvironment environment,
    ILogger<EmailService> logger)
{
    public async Task SendReportSubmittedAsync(FuelReport report, string employeeName)
    {
        await SendReportNotificationAsync(
            report,
            subject: $"Fuel report submitted: {report.ReportDate:yyyy-MM-dd}",
            textIntro: "A fuel report has been submitted.",
            htmlIntro: "A fuel report has been submitted.",
            timestampLabel: "Submitted at (UTC)",
            timestamp: report.SubmittedAtUtc,
            employeeName: employeeName);
    }

    public async Task SendReportCompletedAsync(FuelReport report, string employeeName)
    {
        await SendReportNotificationAsync(
            report,
            subject: $"Fuel report completed: {report.ReportDate:yyyy-MM-dd}",
            textIntro: "A fuel report has been fully completed.",
            htmlIntro: "A fuel report has been fully completed.",
            timestampLabel: "Completed at (UTC)",
            timestamp: report.EndGaugeSignedAtUtc,
            employeeName: employeeName);
    }

    private async Task SendReportNotificationAsync(
        FuelReport report,
        string subject,
        string textIntro,
        string htmlIntro,
        string timestampLabel,
        DateTime? timestamp,
        string employeeName)
    {
        var recipients = await dbContext.NotificationRecipients.Where(x => x.IsActive).ToListAsync();
        if (recipients.Count == 0)
        {
            logger.LogInformation("No notification recipients configured.");
            return;
        }

        var fuelEntries = await dbContext.FuelEntries
            .Where(x => x.FuelReportId == report.Id)
            .Include(x => x.Trailer)
            .OrderBy(x => x.EnteredAtUtc)
            .ToListAsync();

        var options = resendOptions.Value;
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            logger.LogWarning("Resend is not configured. Missing ApiKey.");

            foreach (var recipient in recipients)
            {
                dbContext.EmailLogs.Add(new EmailLog
                {
                    FuelReportId = report.Id,
                    RecipientEmail = recipient.Email,
                    Subject = subject,
                    Status = "Skipped",
                    ErrorMessage = "Resend is not configured (missing ApiKey).",
                    SentAtUtc = DateTime.UtcNow
                });
            }

            await dbContext.SaveChangesAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(options.FromEmail))
        {
            if (environment.IsDevelopment())
            {
                options.FromEmail = ResendOptions.DefaultFromEmail;
                logger.LogWarning("Resend FromEmail was empty. Using development fallback sender {DefaultFromEmail}.", ResendOptions.DefaultFromEmail);
            }
            else
            {
                logger.LogWarning("Resend is not configured. Missing FromEmail in non-development environment.");

                foreach (var recipient in recipients)
                {
                    dbContext.EmailLogs.Add(new EmailLog
                    {
                        FuelReportId = report.Id,
                        RecipientEmail = recipient.Email,
                        Subject = subject,
                        Status = "Skipped",
                        ErrorMessage = "Resend is not configured (missing FromEmail).",
                        SentAtUtc = DateTime.UtcNow
                    });
                }

                await dbContext.SaveChangesAsync();
                return;
            }
        }

        if (!IsValidEmailAddress(options.FromEmail))
        {
            logger.LogWarning("Resend is not configured. Invalid FromEmail format: {FromEmail}", options.FromEmail);

            foreach (var recipient in recipients)
            {
                dbContext.EmailLogs.Add(new EmailLog
                {
                    FuelReportId = report.Id,
                    RecipientEmail = recipient.Email,
                    Subject = subject,
                    Status = "Skipped",
                    ErrorMessage = $"Resend is not configured (invalid FromEmail: {options.FromEmail}).",
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
            try
            {
                var payload = new ResendSendEmailRequest
                {
                    From = BuildFromAddress(options),
                    To = [recipient.Email],
                    Subject = subject,
                    Text = BuildTextBody(report, employeeName, textIntro, timestampLabel, timestamp, fuelEntries),
                    Html = BuildHtmlBody(report, employeeName, htmlIntro, timestampLabel, timestamp, fuelEntries)
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

    private static string BuildFromAddress(ResendOptions options) =>
        string.IsNullOrWhiteSpace(options.FromName)
            ? options.FromEmail.Trim()
            : $"{options.FromName.Trim()} <{options.FromEmail.Trim()}>";

    private static bool IsValidEmailAddress(string email)
    {
        try
        {
            var parsed = new MailAddress(email);
            return parsed.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    private static string BuildTextBody(
        FuelReport report,
        string employeeName,
        string intro,
        string timestampLabel,
        DateTime? timestamp,
        IReadOnlyCollection<FuelEntry> fuelEntries)
    {
        var entryLines = fuelEntries.Count == 0
            ? "None"
            : string.Join(
                Environment.NewLine,
                fuelEntries.Select(entry =>
                    $"- {entry.EnteredAtUtc:yyyy-MM-dd HH:mm:ss} UTC | {entry.FuelType} | {entry.GallonsPumped:0.##} gal | Trailer: {entry.Trailer?.TrailerNumber ?? "N/A"}"));

        return $"""
                {intro}

                Employee: {employeeName}
                Report date: {report.ReportDate:yyyy-MM-dd}
                Report ID: {report.Id}
                Start fuel gauge: {report.FuelingTankLevelStart}
                End fuel gauge: {report.FuelingTankLevelEnd}
                Start gauge supervisor sign-off (UTC): {FormatUtcTimestamp(report.StartGaugeSignedAtUtc)}
                End gauge supervisor sign-off (UTC): {FormatUtcTimestamp(report.EndGaugeSignedAtUtc)}
                {timestampLabel}: {timestamp:yyyy-MM-dd HH:mm:ss}

                Fuel entries:
                {entryLines}

                Please review it in Fuel App.
                """;
    }

    private static string BuildHtmlBody(
        FuelReport report,
        string employeeName,
        string intro,
        string timestampLabel,
        DateTime? timestamp,
        IReadOnlyCollection<FuelEntry> fuelEntries)
    {
        var entryItems = fuelEntries.Count == 0
            ? "<li>None</li>"
            : string.Join(
                string.Empty,
                fuelEntries.Select(entry =>
                    $"<li>{entry.EnteredAtUtc:yyyy-MM-dd HH:mm:ss} UTC | {System.Net.WebUtility.HtmlEncode(entry.FuelType.ToString())} | {entry.GallonsPumped:0.##} gal | Trailer: {System.Net.WebUtility.HtmlEncode(entry.Trailer?.TrailerNumber ?? "N/A")}</li>"));

        return $"""
                <p>{System.Net.WebUtility.HtmlEncode(intro)}</p>
                <ul>
                  <li><strong>Employee:</strong> {System.Net.WebUtility.HtmlEncode(employeeName)}</li>
                  <li><strong>Report date:</strong> {report.ReportDate:yyyy-MM-dd}</li>
                  <li><strong>Report ID:</strong> {report.Id}</li>
                  <li><strong>Start fuel gauge:</strong> {report.FuelingTankLevelStart}</li>
                  <li><strong>End fuel gauge:</strong> {report.FuelingTankLevelEnd}</li>
                  <li><strong>Start gauge supervisor sign-off (UTC):</strong> {System.Net.WebUtility.HtmlEncode(FormatUtcTimestamp(report.StartGaugeSignedAtUtc))}</li>
                  <li><strong>End gauge supervisor sign-off (UTC):</strong> {System.Net.WebUtility.HtmlEncode(FormatUtcTimestamp(report.EndGaugeSignedAtUtc))}</li>
                  <li><strong>{System.Net.WebUtility.HtmlEncode(timestampLabel)}:</strong> {timestamp:yyyy-MM-dd HH:mm:ss}</li>
                </ul>
                <p><strong>Fuel entries:</strong></p>
                <ul>
                  {entryItems}
                </ul>
                <p>Please review it in Fuel App.</p>
                """;
    }

    private static string FormatUtcTimestamp(DateTime? timestamp) =>
        timestamp.HasValue ? $"{timestamp.Value:yyyy-MM-dd HH:mm:ss}" : "Not signed";
}
