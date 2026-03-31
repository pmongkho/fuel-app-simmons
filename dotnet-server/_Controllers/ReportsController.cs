using System.Security.Claims;
using dotnet_server._Data;
using dotnet_server.Application.DTOs;
using dotnet_server.Application.Services;
using dotnet_server.Domain.Entities;
using dotnet_server.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController(AppDbContext dbContext, EmailService emailService, GaugeOcrService gaugeOcrService) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool CanAccessAllReports() =>
        User.IsInRole(nameof(UserRole.Admin)) || User.IsInRole(nameof(UserRole.Supervisor));

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Employee)},{nameof(UserRole.Supervisor)},{nameof(UserRole.Admin)}")]
    public async Task<IActionResult> Create([FromBody] CreateReportRequest request)
    {
        if (request.FuelingTankLevelStart is null)
            return BadRequest("Overall fueling tank start is required.");
        if (request.FuelingTankLevelStart is < 0 or > 999999)
            return BadRequest("Overall fueling tank start must be between 0 and 999999.");

        var fuelingTankLevelEnd = request.FuelingTankLevelEnd ?? request.FuelingTankLevelStart.Value;
        if (fuelingTankLevelEnd is < 0 or > 999999)
            return BadRequest("Overall fueling tank end must be between 0 and 999999.");
        if (fuelingTankLevelEnd < request.FuelingTankLevelStart.Value)
            return BadRequest("Overall fueling tank end must be greater than or equal to start.");

        var userId = CurrentUserId;
        var report = new FuelReport
        {
            ReportDate = request.ReportDate,
            CreatedByUserId = userId,
            FuelingTankLevelStart = request.FuelingTankLevelStart.Value,
            FuelingTankLevelEnd = fuelingTankLevelEnd,
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.FuelReports.Add(report);
        await dbContext.SaveChangesAsync();
        return Ok(new { report.Id, status = report.Status.ToString() });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var report = await dbContext.FuelReports
            .Include(x => x.CreatedByUser)
            .Include(x => x.Entries)
            .ThenInclude(e => e.EnteredByUser)
            .Include(x => x.Entries)
            .ThenInclude(e => e.Trailer)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (report is null) return NotFound();
        if (!CanAccessAllReports() && report.CreatedByUserId != CurrentUserId) return Forbid();

        return Ok(new
        {
            report.Id,
            report.ReportDate,
            createdBy = report.CreatedByUser != null ? report.CreatedByUser.FullName : string.Empty,
            status = report.Status.ToString(),
            report.TotalRedDiesel,
            report.TotalClearDiesel,
            report.TotalDef,
            report.OverallTotalGallons,
            report.FuelingTankLevelStart,
            report.FuelingTankLevelEnd,
            report.StartGaugeSignedBySupervisorId,
            report.StartGaugeSignedAtUtc,
            report.StartGaugeSupervisorSignatureName,
            report.EndGaugeSignedBySupervisorId,
            report.EndGaugeSignedAtUtc,
            report.EndGaugeSupervisorSignatureName,
            report.SubmittedAtUtc,
            entries = report.Entries
                .OrderBy(x => x.EnteredAtUtc)
                .Select(x => new
                {
                    x.Id,
                    fuelType = x.FuelType.ToString(),
                    x.GallonsPumped,
                    verificationStatus = x.VerificationStatus.ToString(),
                    x.EnteredAtUtc,
                    enteredBy = x.EnteredByUser != null ? x.EnteredByUser.FullName : string.Empty,
                    trailerNumber = x.Trailer != null ? x.Trailer.TrailerNumber : string.Empty
                })
        });
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        if (CanAccessAllReports())
        {
            return Ok(await dbContext.FuelReports
                .Include(x => x.CreatedByUser)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new
                {
                    x.Id,
                    x.ReportDate,
                    createdBy = x.CreatedByUser != null ? x.CreatedByUser.FullName : string.Empty,
                    status = x.Status.ToString(),
                    x.FuelingTankLevelStart,
                    x.FuelingTankLevelEnd,
                    x.OverallTotalGallons,
                    x.CreatedAtUtc,
                    x.SubmittedAtUtc
                })
                .ToListAsync());
        }

        var userId = CurrentUserId;
        return Ok(await dbContext.FuelReports
            .Where(x => x.CreatedByUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.ReportDate,
                status = x.Status.ToString(),
                x.FuelingTankLevelStart,
                x.FuelingTankLevelEnd,
                x.OverallTotalGallons,
                x.CreatedAtUtc,
                x.SubmittedAtUtc
            })
            .ToListAsync());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateReportRequest request)
    {
        var report = await dbContext.FuelReports.FindAsync(id);
        if (report is null) return NotFound();
        if (!CanAccessAllReports() && report.CreatedByUserId != CurrentUserId) return Forbid();
        if (request.FuelingTankLevelStart is null || request.FuelingTankLevelEnd is null)
            return BadRequest("Overall fueling tank start and end are required.");
        if (request.FuelingTankLevelStart is < 0 or > 999999 || request.FuelingTankLevelEnd is < 0 or > 999999)
            return BadRequest("Overall fueling tank levels must be between 0 and 999999.");
        if (request.FuelingTankLevelEnd < request.FuelingTankLevelStart)
            return BadRequest("Overall fueling tank end must be greater than or equal to start.");

        report.ReportDate = request.ReportDate;
        report.FuelingTankLevelStart = request.FuelingTankLevelStart.Value;
        report.FuelingTankLevelEnd = request.FuelingTankLevelEnd.Value;
        await dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var report = await dbContext.FuelReports.Include(x => x.Entries).FirstOrDefaultAsync(x => x.Id == id);
        if (report is null) return NotFound();
        if (!CanAccessAllReports() && report.CreatedByUserId != CurrentUserId) return Forbid();
        if (report.Status != FuelReportStatus.Draft) return BadRequest("Only draft reports can be deleted.");
        if (report.StartGaugeSignedBySupervisorId is not null || report.EndGaugeSignedBySupervisorId is not null)
            return BadRequest("Signed reports cannot be deleted.");

        dbContext.FuelReports.Remove(report);
        await dbContext.SaveChangesAsync();
        return Ok(new { message = "Draft report deleted." });
    }

    [HttpPost("extract-gauge-reading")]
    [Authorize(Roles = $"{nameof(UserRole.Employee)},{nameof(UserRole.Supervisor)},{nameof(UserRole.Admin)}")]
    public async Task<IActionResult> ExtractGaugeReading([FromForm] IFormFile? file)
    {
        if (file is null || file.Length == 0) return BadRequest("File is required.");

        try
        {
            await using var fileStream = file.OpenReadStream();
            var (reading, rawText) = await gaugeOcrService.ExtractGaugeReadingAsync(fileStream, HttpContext.RequestAborted);
            return Ok(new { reading, rawText });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (TimeoutException)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, "OCR processing timed out.");
        }
    }

    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        var report = await dbContext.FuelReports.Include(x => x.Entries).FirstOrDefaultAsync(x => x.Id == id);
        if (report is null) return NotFound();
        if (!CanAccessAllReports() && report.CreatedByUserId != CurrentUserId) return Forbid();
        if (report.Entries.Count == 0) return BadRequest("Report must have entries");

        ReportTotalsService.Recalculate(report);
        var expectedTotalGallons = report.FuelingTankLevelEnd - report.FuelingTankLevelStart;
        if (expectedTotalGallons != report.OverallTotalGallons)
            return BadRequest("Overall fueling tank levels must match total gallons pumped on entries.");

        report.SubmittedAtUtc = DateTime.UtcNow;
        var shouldMarkCompleted = report.EndGaugeSignedBySupervisorId is not null;
        report.Status = shouldMarkCompleted ? FuelReportStatus.Completed : FuelReportStatus.Submitted;
        await dbContext.SaveChangesAsync();

        var employee = await dbContext.Users.FindAsync(report.CreatedByUserId);
        var employeeName = employee?.FullName ?? "Employee";

        if (shouldMarkCompleted)
        {
            await emailService.SendReportCompletedAsync(report, employeeName);
            return Ok(new { message = "Report submitted and completed successfully." });
        }

        await emailService.SendReportSubmittedAsync(report, employeeName);
        return Ok(new { message = "Report submitted successfully." });
    }
}
