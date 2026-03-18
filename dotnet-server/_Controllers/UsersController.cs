using dotnet_server._Data;
using dotnet_server.Application.DTOs;
using dotnet_server.Domain.Entities;
using dotnet_server.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class UsersController(AppDbContext dbContext, UserManager<User> userManager) : ControllerBase
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
            UserName = request.Email,
            Email = request.Email,
            Role = role,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new { errors = createResult.Errors.Select(e => e.Description).ToList() });
        }

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

    [HttpPut("{id:int}/email")]
    public async Task<IActionResult> UpdateEmail(int id, [FromBody] UpdateUserEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest("Email is required");

        var normalizedEmail = request.Email.Trim();
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        var existingUser = await userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is not null && existingUser.Id != user.Id)
        {
            return BadRequest("Email is already in use");
        }

        user.Email = normalizedEmail;
        user.UserName = normalizedEmail;
        user.NormalizedEmail = userManager.NormalizeEmail(normalizedEmail);
        user.NormalizedUserName = userManager.NormalizeName(normalizedEmail);

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description).ToList() });
        }

        return Ok();
    }

    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> UpdatePassword(int id, [FromBody] UpdateUserPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword)) return BadRequest("NewPassword is required");

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description).ToList() });
        }

        return Ok();
    }
}
