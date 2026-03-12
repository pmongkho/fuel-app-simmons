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
    public async Task<IActionResult> Pending([FromQuery] DateOnly? date)
    {
        var query = dbContext.FuelEntries.Include(x => x.FuelReport).Include(x => x.EnteredByUser).Where(x => x.VerificationStatus == VerificationStatus.Pending);
        if (date.HasValue) query = query.Where(x => x.FuelReport!.ReportDate == date.Value);

        return Ok(await query.OrderBy(x => x.EnteredAtUtc).Select(x => new
        {
            x.Id,
            reportDate = x.FuelReport!.ReportDate,
            employee = x.EnteredByUser!.FullName,
            x.TrailerNumber,
            fuelType = x.FuelType.ToString(),
            x.GallonsPumped,
            submittedTime = x.EnteredAtUtc,
            status = x.VerificationStatus.ToString()
        }).ToListAsync());
    }

    [HttpGet("entries/{entryId:int}")]
    public async Task<IActionResult> Get(int entryId)
    {
        var entry = await dbContext.FuelEntries.Include(x => x.Photos).Include(x => x.FuelReport).Include(x => x.EnteredByUser).FirstOrDefaultAsync(x => x.Id == entryId);
        return entry is null ? NotFound() : Ok(entry);
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
        entry.RejectionReason = request.RejectionReason;

        ReportTotalsService.Recalculate(entry.FuelReport!);
        await dbContext.SaveChangesAsync();
        return Ok(new { message = "Entry rejected." });
    }
}
