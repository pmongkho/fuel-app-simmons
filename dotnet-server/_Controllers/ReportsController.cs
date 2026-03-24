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
public class ReportsController(AppDbContext dbContext, EmailService emailService) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool CanAccessAllReports() =>
        User.IsInRole(nameof(UserRole.Admin)) || User.IsInRole(nameof(UserRole.Supervisor));

    private async Task<FuelReport?> FindAccessibleReportAsync(int id)
    {
        var report = await dbContext.FuelReports
            .Include(x => x.Entries)
            .ThenInclude(e => e.Photos)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (report is null) return null;
        if (CanAccessAllReports() || report.CreatedByUserId == CurrentUserId) return report;

        return null;
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Employee)},{nameof(UserRole.Supervisor)},{nameof(UserRole.Admin)}")]
    public async Task<IActionResult> Create([FromBody] CreateReportRequest request)
    {
        if (request.FuelingTankLevelStart is null || request.FuelingTankLevelEnd is null)
            return BadRequest("Overall fueling tank start and end are required.");
        if (request.FuelingTankLevelStart is < 0 or > 999999 || request.FuelingTankLevelEnd is < 0 or > 999999)
            return BadRequest("Overall fueling tank levels must be between 0 and 999999.");
        if (request.FuelingTankLevelEnd < request.FuelingTankLevelStart)
            return BadRequest("Overall fueling tank end must be greater than or equal to start.");

        var userId = CurrentUserId;
        var report = new FuelReport
        {
            ReportDate = request.ReportDate,
            CreatedByUserId = userId,
            FuelingTankLevelStart = request.FuelingTankLevelStart.Value,
            FuelingTankLevelEnd = request.FuelingTankLevelEnd.Value,
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.FuelReports.Add(report);
        await dbContext.SaveChangesAsync();
        return Ok(new { report.Id, status = report.Status.ToString() });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var report = await FindAccessibleReportAsync(id);
        return report is null ? NotFound() : Ok(report);
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

        report.Status = FuelReportStatus.Submitted;
        report.SubmittedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var employee = await dbContext.Users.FindAsync(report.CreatedByUserId);
        await emailService.SendReportSubmittedAsync(report, employee?.FullName ?? "Employee");
        return Ok(new { message = "Report submitted successfully." });
    }
}
