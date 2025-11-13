using AuthService.DTOs.Auth;

namespace AuthService.DTOs.Responses;

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public UserDto? User { get; set; }
}
