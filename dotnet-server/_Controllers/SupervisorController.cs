using System.Security.Claims;
using dotnet_server._Data;
using dotnet_server.Application.DTOs;
using dotnet_server.Application.Services;
using dotnet_server.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server.Api.Controllers;

[ApiController]
[Route("api/supervisor")]
[Authorize(Roles = $"{nameof(UserRole.Supervisor)},{nameof(UserRole.Admin)}")]
public class SupervisorController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("entries/pending")]
    public async Task<IActionResult> Pending([FromQuery] string? date)
    {
        var query = dbContext.FuelEntries
            .Include(x => x.FuelReport)
            .ThenInclude(r => r!.Entries)
            .Include(x => x.EnteredByUser)
            .Include(x => x.Trailer)
            .Where(x => x.VerificationStatus == VerificationStatus.Pending);
        if (!string.IsNullOrWhiteSpace(date))
        {
            if (!DateOnly.TryParse(date, out var parsedDate)) return BadRequest("Invalid date format. Use yyyy-MM-dd.");
            query = query.Where(x => x.FuelReport!.ReportDate == parsedDate);
        }

        return Ok(await query.OrderBy(x => x.EnteredAtUtc).Select(x => new
        {
            x.Id,
            reportId = x.FuelReport!.Id,
            reportDate = x.FuelReport!.ReportDate,
            reportFuelingTankLevelStart = x.FuelReport!.FuelingTankLevelStart,
            reportFuelingTankLevelEnd = x.FuelReport!.FuelingTankLevelEnd,
            reportCreatedByUserId = x.FuelReport!.CreatedByUserId,
            reportStatus = x.FuelReport!.Status.ToString(),
            reportTotalRedDiesel = x.FuelReport!.TotalRedDiesel,
            reportTotalClearDiesel = x.FuelReport!.TotalClearDiesel,
            reportTotalDef = x.FuelReport!.TotalDef,
            reportOverallTotalGallons = x.FuelReport!.OverallTotalGallons,
            reportCreatedAtUtc = x.FuelReport!.CreatedAtUtc,
            reportSubmittedAtUtc = x.FuelReport!.SubmittedAtUtc,
            reportStartGaugeSignedBySupervisorId = x.FuelReport!.StartGaugeSignedBySupervisorId,
            reportEndGaugeSignedBySupervisorId = x.FuelReport!.EndGaugeSignedBySupervisorId,
            reportEntriesCount = x.FuelReport!.Entries.Count,
            employee = x.EnteredByUser!.FullName,
            trailerNumber = x.Trailer != null ? x.Trailer.TrailerNumber : string.Empty,
            fuelType = x.FuelType.ToString(),
            x.GallonsPumped,
            submittedTime = x.EnteredAtUtc,
            status = x.VerificationStatus.ToString()
        }).ToListAsync());
    }


    [HttpGet("reports/{reportId:int}")]
    public async Task<IActionResult> Report(int reportId)
    {
        var report = await dbContext.FuelReports
            .Include(x => x.CreatedByUser)
            .Include(x => x.Entries)
            .ThenInclude(e => e.Photos)
            .Include(x => x.Entries)
            .ThenInclude(e => e.EnteredByUser)
            .Include(x => x.Entries)
            .ThenInclude(e => e.Trailer)
            .FirstOrDefaultAsync(x => x.Id == reportId);

        if (report is null) return NotFound();

        return Ok(new
        {
            report.Id,
            report.ReportDate,
            report.CreatedByUserId,
            createdBy = report.CreatedByUser!.FullName,
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
            report.CreatedAtUtc,
            report.SubmittedAtUtc,
            entriesCount = report.Entries.Count,
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
                    trailerNumber = x.Trailer != null ? x.Trailer.TrailerNumber : string.Empty,
                    photoCount = x.Photos.Count
                })
        });
    }
    [HttpGet("entries/{entryId:int}")]
    public async Task<IActionResult> Get(int entryId)
    {
        var entry = await dbContext.FuelEntries
            .Include(x => x.Photos)
            .Include(x => x.FuelReport)
            .ThenInclude(report => report!.Entries)
            .ThenInclude(reportEntry => reportEntry.EnteredByUser)
            .Include(x => x.FuelReport)
            .ThenInclude(report => report!.CreatedByUser)
            .Include(x => x.FuelReport)
            .ThenInclude(report => report!.Entries)
            .ThenInclude(reportEntry => reportEntry.Trailer)
            .Include(x => x.EnteredByUser)
            .Include(x => x.Trailer)
            .FirstOrDefaultAsync(x => x.Id == entryId);

        if (entry is null || entry.FuelReport is null) return NotFound();

        return Ok(new
        {
            entry.Id,
            fuelType = entry.FuelType.ToString(),
            entry.GallonsPumped,
            notes = entry.Trailer != null ? entry.Trailer.Notes : null,
            verificationStatus = entry.VerificationStatus.ToString(),
            entry.EnteredAtUtc,
            enteredBy = entry.EnteredByUser != null ? entry.EnteredByUser.FullName : string.Empty,
            trailerNumber = entry.Trailer != null ? entry.Trailer.TrailerNumber : string.Empty,
            report = new
            {
                entry.FuelReport.Id,
                entry.FuelReport.ReportDate,
                entry.FuelReport.FuelingTankLevelStart,
                entry.FuelReport.FuelingTankLevelEnd,
                createdBy = entry.FuelReport.CreatedByUser != null ? entry.FuelReport.CreatedByUser.FullName : string.Empty,
                status = entry.FuelReport.Status.ToString(),
                entriesCount = entry.FuelReport.Entries.Count
            },
            reportEntries = entry.FuelReport.Entries
                .OrderBy(reportEntry => reportEntry.EnteredAtUtc)
                .Select(reportEntry => new
                {
                    reportEntry.Id,
                    fuelType = reportEntry.FuelType.ToString(),
                    reportEntry.GallonsPumped,
                    verificationStatus = reportEntry.VerificationStatus.ToString(),
                    reportEntry.EnteredAtUtc,
                    enteredBy = reportEntry.EnteredByUser != null ? reportEntry.EnteredByUser.FullName : string.Empty,
                    trailerNumber = reportEntry.Trailer != null ? reportEntry.Trailer.TrailerNumber : string.Empty
                })
        });
    }

    [HttpPost("entries/{entryId:int}/approve")]
    public async Task<IActionResult> Approve(int entryId, [FromBody] ApproveEntryRequest request)
    {
        var entry = await dbContext.FuelEntries.Include(x => x.FuelReport).ThenInclude(r => r!.Entries).FirstOrDefaultAsync(x => x.Id == entryId);
        if (entry is null) return NotFound();

        entry.VerificationStatus = VerificationStatus.Approved;
        entry.VerifiedBySupervisorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        entry.VerifiedAtUtc = DateTime.UtcNow;
        entry.SupervisorSignatureName = request.SignatureName;
        entry.RejectionReason = null;

        ReportTotalsService.Recalculate(entry.FuelReport!);
        await dbContext.SaveChangesAsync();
        return Ok(new { message = "Entry approved successfully." });
    }

    [HttpPost("entries/{entryId:int}/reject")]
    public async Task<IActionResult> Reject(int entryId, [FromBody] RejectEntryRequest request)
    {
        var entry = await dbContext.FuelEntries.Include(x => x.FuelReport).ThenInclude(r => r!.Entries).FirstOrDefaultAsync(x => x.Id == entryId);
        if (entry is null) return NotFound();

        entry.VerificationStatus = VerificationStatus.Rejected;
        entry.VerifiedBySupervisorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        entry.VerifiedAtUtc = DateTime.UtcNow;
        entry.SupervisorSignatureName = request.SignatureName;
        entry.RejectionReason = request.RejectionReason;

        ReportTotalsService.Recalculate(entry.FuelReport!);
        await dbContext.SaveChangesAsync();
        return Ok(new { message = "Entry rejected." });
    }

    [HttpPost("reports/{reportId:int}/signoff-start")]
    public async Task<IActionResult> SignOffStartGauge(int reportId, [FromBody] SignOffReportGaugeRequest request)
    {
        var report = await dbContext.FuelReports.FindAsync(reportId);
        if (report is null) return NotFound();

        var supervisorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        report.StartGaugeSignedBySupervisorId = supervisorId;
        report.StartGaugeSupervisorSignatureName = request.SignatureName;
        report.StartGaugeSignedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Ok(new { message = "Start gauge signed successfully." });
    }

    [HttpPost("reports/{reportId:int}/signoff-end")]
    public async Task<IActionResult> SignOffEndGauge(int reportId, [FromBody] SignOffReportGaugeRequest request)
    {
        var report = await dbContext.FuelReports.FindAsync(reportId);
        if (report is null) return NotFound();

        var supervisorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (report.StartGaugeSignedBySupervisorId is null)
            return BadRequest("Start gauge sign-off is required before end gauge sign-off.");
        if (report.StartGaugeSignedBySupervisorId == supervisorId)
            return BadRequest("End gauge sign-off must be completed by a different supervisor.");

        report.EndGaugeSignedBySupervisorId = supervisorId;
        report.EndGaugeSupervisorSignatureName = request.SignatureName;
        report.EndGaugeSignedAtUtc = DateTime.UtcNow;

        if (report.Status == FuelReportStatus.Submitted)
            report.Status = FuelReportStatus.Completed;

        await dbContext.SaveChangesAsync();

        return Ok(new { message = "End gauge signed successfully." });
    }
}
