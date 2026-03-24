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
[Route("api")]
[Authorize]
public class EntriesController(AppDbContext dbContext) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool CanAccessAllEntries() =>
        User.IsInRole(nameof(UserRole.Admin)) || User.IsInRole(nameof(UserRole.Supervisor));

    private bool CanModifyEntry(FuelEntry entry) =>
        CanAccessAllEntries() || entry.EnteredByUserId == CurrentUserId;

    private async Task<(Trailer? trailer, IActionResult? errorResult)> ResolveAndUpdateTrailerAsync(CreateFuelEntryRequest request)
    {
        var normalizedTrailerNumber = request.TrailerNumber.Trim();
        if (string.IsNullOrWhiteSpace(normalizedTrailerNumber))
            return (null, BadRequest("Trailer number is required."));

        Trailer? trailer = null;

        if (request.TrailerId is int trailerId)
        {
            trailer = await dbContext.Trailers.FirstOrDefaultAsync(x => x.Id == trailerId && x.IsActive);
            if (trailer is not null && !string.Equals(trailer.TrailerNumber, normalizedTrailerNumber, StringComparison.OrdinalIgnoreCase))
                trailer = null;
        }

        trailer ??= await dbContext.Trailers.FirstOrDefaultAsync(x => x.TrailerNumber == normalizedTrailerNumber && x.IsActive);

        if (trailer is null)
        {
            trailer = new Trailer
            {
                TrailerNumber = normalizedTrailerNumber,
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.Trailers.Add(trailer);
        }

        trailer.IsTankFull = request.IsTankFull;
        trailer.HasMechanicalIssues = request.HasMechanicalIssues;
        trailer.Notes = request.TrailerNotes;
        trailer.UpdatedAtUtc = DateTime.UtcNow;

        return (trailer, null);
    }

    [HttpPost("reports/{reportId:int}/entries")]
    public async Task<IActionResult> CreateEntry(int reportId, [FromBody] CreateFuelEntryRequest request)
    {
        if (!Enum.TryParse<FuelType>(request.FuelType, true, out var fuelType)) return BadRequest("Invalid fuel type");

        var (trailer, trailerErrorResult) = await ResolveAndUpdateTrailerAsync(request);
        if (trailerErrorResult is not null) return trailerErrorResult;

        var report = await dbContext.FuelReports.Include(x => x.Entries).FirstOrDefaultAsync(x => x.Id == reportId);
        if (report is null) return NotFound();
        if (!CanAccessAllEntries() && report.CreatedByUserId != CurrentUserId) return Forbid();

        var entry = new FuelEntry
        {
            FuelReportId = reportId,
            Trailer = trailer,
            FuelType = fuelType,
            GallonsPumped = request.GallonsPumped,
            EnteredByUserId = CurrentUserId,
            EnteredAtUtc = DateTime.UtcNow
        };

        report.Entries.Add(entry);
        ReportTotalsService.Recalculate(report);
        await dbContext.SaveChangesAsync();
        return Ok(new { entry.Id, verificationStatus = entry.VerificationStatus.ToString() });
    }

    [HttpPut("entries/{entryId:int}")]
    public async Task<IActionResult> EditEntry(int entryId, [FromBody] CreateFuelEntryRequest request)
    {
        if (!Enum.TryParse<FuelType>(request.FuelType, true, out var fuelType)) return BadRequest("Invalid fuel type");

        var (trailer, trailerErrorResult) = await ResolveAndUpdateTrailerAsync(request);
        if (trailerErrorResult is not null) return trailerErrorResult;

        var entry = await dbContext.FuelEntries.Include(x => x.FuelReport).ThenInclude(r => r!.Entries).FirstOrDefaultAsync(x => x.Id == entryId);
        if (entry is null) return NotFound();
        if (!CanModifyEntry(entry)) return Forbid();
        if (entry.VerificationStatus == VerificationStatus.Approved && !User.IsInRole(nameof(UserRole.Admin))) return BadRequest("Approved entries cannot be edited");

        entry.Trailer = trailer;
        entry.FuelType = fuelType;
        entry.GallonsPumped = request.GallonsPumped;

        ReportTotalsService.Recalculate(entry.FuelReport!);
        await dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("entries/{entryId:int}")]
    public async Task<IActionResult> DeleteEntry(int entryId)
    {
        var entry = await dbContext.FuelEntries.Include(x => x.FuelReport).ThenInclude(r => r!.Entries).FirstOrDefaultAsync(x => x.Id == entryId);
        if (entry is null) return NotFound();
        if (!CanModifyEntry(entry)) return Forbid();
        if (entry.VerificationStatus == VerificationStatus.Approved) return BadRequest("Approved entries cannot be deleted");

        dbContext.FuelEntries.Remove(entry);
        ReportTotalsService.Recalculate(entry.FuelReport!);
        await dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("entries/{entryId:int}")]
    public async Task<IActionResult> GetEntry(int entryId)
    {
        var entry = await dbContext.FuelEntries.Include(x => x.Photos).Include(x => x.Trailer).FirstOrDefaultAsync(x => x.Id == entryId);
        if (entry is null) return NotFound();
        if (!CanModifyEntry(entry)) return Forbid();

        return Ok(entry);
    }

    [HttpPost("entries/{entryId:int}/photos")]
    public async Task<IActionResult> UploadPhoto(int entryId, [FromForm] string photoType, IFormFile file)
    {
        if (!Enum.TryParse<FuelPhotoType>(photoType, true, out var parsedType)) return BadRequest("Invalid photo type");
        var entry = await dbContext.FuelEntries.FindAsync(entryId);
        if (entry is null) return NotFound();
        if (!CanModifyEntry(entry)) return Forbid();

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "fuel");
        Directory.CreateDirectory(uploadsDir);
        var safeName = $"entry-{entryId}-{Guid.NewGuid():N}-{file.FileName}";
        var fullPath = Path.Combine(uploadsDir, safeName);
        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);

        var photo = new FuelEntryPhoto
        {
            FuelEntryId = entryId,
            PhotoType = parsedType,
            FileName = file.FileName,
            FilePath = $"/uploads/fuel/{safeName}",
            ContentType = file.ContentType,
            UploadedAtUtc = DateTime.UtcNow
        };

        dbContext.FuelEntryPhotos.Add(photo);
        await dbContext.SaveChangesAsync();
        return Ok(new { photo.Id, photoType = photo.PhotoType.ToString(), photo.FilePath });
    }

    [HttpGet("entries/{entryId:int}/photos")]
    public async Task<IActionResult> GetPhotos(int entryId)
    {
        var entry = await dbContext.FuelEntries.FindAsync(entryId);
        if (entry is null) return NotFound();
        if (!CanModifyEntry(entry)) return Forbid();

        var photos = await dbContext.FuelEntryPhotos.Where(x => x.FuelEntryId == entryId).ToListAsync();
        return Ok(photos);
    }
}
