using dotnet_server._Data;
using dotnet_server.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
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
    public async Task<IActionResult> Reports([FromQuery] DateOnly? date, [FromQuery] string? status)
    {
        var query = dbContext.FuelReports.Include(x => x.CreatedByUser).AsQueryable();
        if (date.HasValue) query = query.Where(x => x.ReportDate == date.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<FuelReportStatus>(status, true, out var parsed)) query = query.Where(x => x.Status == parsed);

        return Ok(await query.OrderByDescending(x => x.CreatedAtUtc).Select(x => new
        {
            x.Id,
            reportDate = x.ReportDate,
            createdBy = x.CreatedByUser!.FullName,
            status = x.Status.ToString(),
            x.OverallTotalGallons,
            x.CreatedAtUtc
        }).ToListAsync());
    }

    [HttpGet("reports/{reportId:int}")]
    public async Task<IActionResult> Report(int reportId)
    {
        var report = await dbContext.FuelReports.Include(x => x.CreatedByUser).Include(x => x.Entries).ThenInclude(e => e.Photos).FirstOrDefaultAsync(x => x.Id == reportId);
        return report is null ? NotFound() : Ok(report);
    }
}
