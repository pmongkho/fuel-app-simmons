namespace dotnet_server.Application.DTOs;

public record LoginRequest(string Email, string Password);

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
    public DateTime ExpiresAtUtc { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UpdateUserEmailRequest
{
    public string Email { get; set; } = string.Empty;
}

public class UpdateUserPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
