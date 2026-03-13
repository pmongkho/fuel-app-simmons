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
[Route("api")]
[Authorize]
public class EntriesController(AppDbContext dbContext) : ControllerBase
{
    private async Task<IActionResult?> ValidateAndUpdateTrailerAsync(CreateFuelEntryRequest request)
    {
        var trailer = await dbContext.Trailers.FirstOrDefaultAsync(x => x.Id == request.TrailerId && x.IsActive);
        if (trailer is null) return NotFound("Trailer not found or inactive.");

        trailer.IsTankFull = request.IsTankFull;
        trailer.HasMechanicalIssues = request.HasMechanicalIssues;
        trailer.Notes = request.TrailerNotes;
        trailer.UpdatedAtUtc = DateTime.UtcNow;
        return null;
    }

    private static bool TankLevelsMatchGallonsPumped(CreateFuelEntryRequest request)
    {
        if (request.FuelingTankLevelStart is null || request.FuelingTankLevelEnd is null) return false;
        var expectedGallons = request.FuelingTankLevelStart.Value - request.FuelingTankLevelEnd.Value;
        return expectedGallons == request.GallonsPumped;
    }

    [HttpPost("reports/{reportId:int}/entries")]
    public async Task<IActionResult> CreateEntry(int reportId, [FromBody] CreateFuelEntryRequest request)
    {
        if (!Enum.TryParse<FuelType>(request.FuelType, true, out var fuelType)) return BadRequest("Invalid fuel type");
        if (request.FuelingTankLevelStart is < 0 or > 999999 || request.FuelingTankLevelEnd is < 0 or > 999999)
            return BadRequest("Fueling tank levels must be between 0 and 999999.");
        if (!TankLevelsMatchGallonsPumped(request))
            return BadRequest("Fueling tank start and finish must match gallons pumped (start - finish = gallons pumped).");

        var trailerValidationResult = await ValidateAndUpdateTrailerAsync(request);
        if (trailerValidationResult is not null) return trailerValidationResult;

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var report = await dbContext.FuelReports.Include(x => x.Entries).FirstOrDefaultAsync(x => x.Id == reportId);
        if (report is null) return NotFound();

        var entry = new Domain.Entities.FuelEntry
        {
            FuelReportId = reportId,
            TrailerId = request.TrailerId,
            FuelType = fuelType,
            FuelingTankLevelStart = request.FuelingTankLevelStart,
            FuelingTankLevelEnd = request.FuelingTankLevelEnd,
            GallonsPumped = request.GallonsPumped,
            EnteredByUserId = userId,
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
        if (request.FuelingTankLevelStart is < 0 or > 999999 || request.FuelingTankLevelEnd is < 0 or > 999999)
            return BadRequest("Fueling tank levels must be between 0 and 999999.");
        if (!TankLevelsMatchGallonsPumped(request))
            return BadRequest("Fueling tank start and finish must match gallons pumped (start - finish = gallons pumped).");

        var trailerValidationResult = await ValidateAndUpdateTrailerAsync(request);
        if (trailerValidationResult is not null) return trailerValidationResult;

        var entry = await dbContext.FuelEntries.Include(x => x.FuelReport).ThenInclude(r => r!.Entries).FirstOrDefaultAsync(x => x.Id == entryId);
        if (entry is null) return NotFound();
        if (entry.VerificationStatus == VerificationStatus.Approved && !User.IsInRole(nameof(UserRole.Admin))) return BadRequest("Approved entries cannot be edited");

        entry.TrailerId = request.TrailerId;
        entry.FuelType = fuelType;
        entry.FuelingTankLevelStart = request.FuelingTankLevelStart;
        entry.FuelingTankLevelEnd = request.FuelingTankLevelEnd;
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
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPost("entries/{entryId:int}/photos")]
    public async Task<IActionResult> UploadPhoto(int entryId, [FromForm] string photoType, IFormFile file)
    {
        if (!Enum.TryParse<FuelPhotoType>(photoType, true, out var parsedType)) return BadRequest("Invalid photo type");
        var entry = await dbContext.FuelEntries.FindAsync(entryId);
        if (entry is null) return NotFound();

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "fuel");
        Directory.CreateDirectory(uploadsDir);
        var safeName = $"entry-{entryId}-{Guid.NewGuid():N}-{file.FileName}";
        var fullPath = Path.Combine(uploadsDir, safeName);
        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);

        var photo = new Domain.Entities.FuelEntryPhoto
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
        var photos = await dbContext.FuelEntryPhotos.Where(x => x.FuelEntryId == entryId).ToListAsync();
        return Ok(photos);
    }
}
