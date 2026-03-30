using dotnet_server._Data;
using dotnet_server.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Supervisor)}")]
public class AdminController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reports = await dbContext.FuelReports.Where(x => x.ReportDate == today).ToListAsync();
        return Ok(new
        {
            reportsToday = reports.Count,
            pendingVerifications = await dbContext.FuelEntries.CountAsync(x => x.VerificationStatus == VerificationStatus.Pending),
            totalRedDiesel = reports.Sum(x => x.TotalRedDiesel),
            totalClearDiesel = reports.Sum(x => x.TotalClearDiesel),
            totalDef = reports.Sum(x => x.TotalDef),
            overallTotalGallons = reports.Sum(x => x.OverallTotalGallons)
        });
    }

    [HttpGet("reports")]
    public async Task<IActionResult> Reports([FromQuery] DateOnly? date = null, [FromQuery] FuelReportStatus? status = null)
    {
        var query = dbContext.FuelReports
            .Include(x => x.CreatedByUser)
            .AsQueryable();

        if (date.HasValue) query = query.Where(x => x.ReportDate == date.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);

        return Ok(await query.OrderByDescending(x => x.CreatedAtUtc).Select(x => new
        {
            x.Id,
            reportDate = x.ReportDate,
            x.CreatedByUserId,
            createdBy = x.CreatedByUser!.FullName,
            status = x.Status.ToString(),
            x.TotalRedDiesel,
            x.TotalClearDiesel,
            x.TotalDef,
            x.OverallTotalGallons,
            x.CreatedAtUtc,
            x.SubmittedAtUtc,
            entriesCount = x.Entries.Count()
        }).ToListAsync());
    }

    [HttpGet("reports/{reportId:int}")]
    public async Task<IActionResult> Report(int reportId)
    {
        var report = await dbContext.FuelReports
            .Include(x => x.CreatedByUser)
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
}
