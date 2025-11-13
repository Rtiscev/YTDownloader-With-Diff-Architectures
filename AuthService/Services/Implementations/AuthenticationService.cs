using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using AuthService.Data;
using AuthService.DTOs.Auth;
using AuthService.DTOs.Responses;
using AuthService.Models;
using AuthService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services.Implementations;

public class AuthenticationService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _context;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ApplicationDbContext context,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var userExists = await _userManager.FindByEmailAsync(request.Email);
            if (userExists != null)
                return new AuthResponse { Success = false, Message = "User already exists" };

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }

            await _userManager.AddToRoleAsync(user, "User");

            return new AuthResponse
            {
                Success = true,
                Message = "User registered successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error");
            return new AuthResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                return new AuthResponse { Success = false, Message = "Invalid credentials" };

            if (!user.IsActive)
                return new AuthResponse { Success = false, Message = "Account is inactive" };

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            refreshToken.UserId = user.Id;
            _context.RefreshTokens.Add(refreshToken);

            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Roles = roles.ToList(),
                CreatedAt = user.CreatedAt
            };

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return new AuthResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
                return new AuthResponse { Success = false, Message = "Invalid access token" };

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId);

            if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiryDate < DateTime.UtcNow)
                return new AuthResponse { Success = false, Message = "Invalid refresh token" };

            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null)
                return new AuthResponse { Success = false, Message = "User not found" };

            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            storedToken.IsRevoked = true;
            newRefreshToken.UserId = user.Id;
            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName ?? "",
                    LastName = user.LastName ?? "",
                    Roles = roles.ToList()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token error");
            return new AuthResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task<bool> LogoutAsync(string userId)
    {
        try
        {
            var tokens = _context.RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked);
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
            return false;
        }
    }

    public async Task<UserDto?> GetUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            Roles = roles.ToList(),
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        try
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName ?? "",
                    LastName = user.LastName ?? "",
                    Roles = roles.ToList(),
                    CreatedAt = user.CreatedAt
                });
            }

            return userDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            // Revoke all refresh tokens for this user
            var tokens = _context.RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked);
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }

            // Delete the user
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            throw;
        }
    }
}
