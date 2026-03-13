using dotnet_server._Data;
using dotnet_server.Application.DTOs;
using dotnet_server.Domain.Entities;
using dotnet_server.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server.Api.Controllers;

[ApiController]
[Route("api/trailers")]
[Authorize]
public class TrailersController(AppDbContext dbContext) : ControllerBase
{
    private static readonly HashSet<string> AllowedLocations = ["Main", "Flex"];

    [HttpGet]
    public async Task<IActionResult> GetTrailers([FromQuery] bool activeOnly = true)
    {
        var query = dbContext.Trailers.AsQueryable();
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var trailers = await query
            .OrderBy(x => x.TrailerNumber)
            .Select(x => new
            {
                x.Id,
                x.TrailerNumber,
                x.Location,
                x.IsTankFull,
                x.HasMechanicalIssues,
                x.Notes,
                x.IsActive,
                x.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(trailers);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Supervisor) + "," + nameof(UserRole.Admin))]
    public async Task<IActionResult> CreateTrailer([FromBody] CreateTrailerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TrailerNumber)) return BadRequest("Trailer number is required.");
        if (!AllowedLocations.Contains(request.Location)) return BadRequest("Trailer location must be Main or Flex.");

        var normalizedNumber = request.TrailerNumber.Trim().ToUpperInvariant();
        var existing = await dbContext.Trailers.FirstOrDefaultAsync(x => x.TrailerNumber == normalizedNumber);
        if (existing is not null)
        {
            return Conflict("Trailer already exists.");
        }

        var trailer = new Trailer
        {
            TrailerNumber = normalizedNumber,
            Location = request.Location,
            IsTankFull = request.IsTankFull,
            HasMechanicalIssues = request.HasMechanicalIssues,
            Notes = request.Notes,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.Trailers.Add(trailer);
        await dbContext.SaveChangesAsync();
        return Ok(new { trailer.Id });
    }

    [HttpPut("{trailerId:int}")]
    [Authorize(Roles = nameof(UserRole.Supervisor) + "," + nameof(UserRole.Admin))]
    public async Task<IActionResult> UpdateTrailer(int trailerId, [FromBody] UpdateTrailerRequest request)
    {
        if (!AllowedLocations.Contains(request.Location)) return BadRequest("Trailer location must be Main or Flex.");

        var trailer = await dbContext.Trailers.FirstOrDefaultAsync(x => x.Id == trailerId);
        if (trailer is null) return NotFound();

        trailer.Location = request.Location;
        trailer.IsTankFull = request.IsTankFull;
        trailer.HasMechanicalIssues = request.HasMechanicalIssues;
        trailer.Notes = request.Notes;
        trailer.IsActive = request.IsActive;
        trailer.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return Ok();
    }
}
