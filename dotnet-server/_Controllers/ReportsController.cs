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
    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Employee)},{nameof(UserRole.Admin)}")]
    public async Task<IActionResult> Create([FromBody] CreateReportRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var report = new FuelReport
        {
            ReportDate = DateOnly.FromDateTime(request.ReportDate),
            CreatedByUserId = userId,
            ReportLocation = request.ReportLocation,
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
            .Include(x => x.Entries).ThenInclude(e => e.Photos)
            .FirstOrDefaultAsync(x => x.Id == id);
        return report is null ? NotFound() : Ok(report);
    }

    [HttpGet("mine")]
    [Authorize(Roles = nameof(UserRole.Employee))]
    public async Task<IActionResult> Mine()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await dbContext.FuelReports.Where(x => x.CreatedByUserId == userId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateReportRequest request)
    {
        var report = await dbContext.FuelReports.FindAsync(id);
        if (report is null) return NotFound();
        report.ReportDate = DateOnly.FromDateTime(request.ReportDate);
        report.ReportLocation = request.ReportLocation;
        await dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        var report = await dbContext.FuelReports.Include(x => x.Entries).FirstOrDefaultAsync(x => x.Id == id);
        if (report is null) return NotFound();
        if (report.Entries.Count == 0) return BadRequest("Report must have entries");

        report.Status = FuelReportStatus.Submitted;
        report.SubmittedAtUtc = DateTime.UtcNow;
        ReportTotalsService.Recalculate(report);
        await dbContext.SaveChangesAsync();

        var employee = await dbContext.Users.FindAsync(report.CreatedByUserId);
        await emailService.SendReportSubmittedAsync(report, employee?.FullName ?? "Employee");
        return Ok(new { message = "Report submitted successfully." });
    }
}
