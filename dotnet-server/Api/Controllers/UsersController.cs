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
[Route("api/users")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class UsersController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers() => Ok(await dbContext.Users.Select(u => new
    {
        u.Id,
        u.FullName,
        u.Email,
        role = u.Role.ToString(),
        u.IsActive
    }).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role)) return BadRequest("Invalid role");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = AuthService.HashPassword(request.Password),
            Role = role,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return Ok(new { user.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await dbContext.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role)) return BadRequest("Invalid role");

        user.Role = role;
        user.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync();
        return Ok();
    }
}
