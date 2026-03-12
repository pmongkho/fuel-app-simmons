using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using dotnet_server._Data;
using dotnet_server.Application.DTOs;
using dotnet_server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace dotnet_server.Application.Services;

public class AuthService(AppDbContext dbContext, IConfiguration configuration)
{
    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == request.Email && x.IsActive);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? "dev-key-dev-key-dev-key-dev-key");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            ]),
            Expires = DateTime.UtcNow.AddHours(12),
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new AuthResponse
        {
            Token = tokenHandler.WriteToken(token),
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString()
            }
        };
    }

    public async Task<UserDto?> GetUserAsync(int userId)
    {
        return await dbContext.Users.Where(x => x.Id == userId && x.IsActive).Select(x => new UserDto
        {
            Id = x.Id,
            FullName = x.FullName,
            Email = x.Email,
            Role = x.Role.ToString()
        }).FirstOrDefaultAsync();
    }

    public static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
}
