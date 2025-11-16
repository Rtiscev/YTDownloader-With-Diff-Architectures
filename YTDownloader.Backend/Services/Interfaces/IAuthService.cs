using YTDownloader.Backend.DTOs.Auth;
using YTDownloader.Backend.DTOs.Responses;

namespace YTDownloader.Backend.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> LogoutAsync(string userId);
    Task<UserDto?> GetUserAsync(string userId);

    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<bool> DeleteUserAsync(string userId);
}
