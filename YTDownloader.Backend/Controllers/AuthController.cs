using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using YTDownloader.Backend.DTOs.Auth;
using YTDownloader.Backend.DTOs.Responses;
using YTDownloader.Backend.Services.Interfaces;

namespace YTDownloader.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _authService.RegisterAsync(request);

        if (response.Success)
            _logger.LogInformation("✓ User registered: {Email}", request.Email);
        else
            _logger.LogWarning("✗ Registration failed: {Email} - {Message}", request.Email, response.Message);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _authService.LoginAsync(request);

        if (response.Success)
            _logger.LogInformation("✓ Login: {Email}", request.Email);
        else
            _logger.LogWarning("✗ Login failed: {Email}", request.Email);

        return response.Success ? Ok(response) : Unauthorized(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var user = await _authService.GetUserAsync(userId);
        return user != null ? Ok(user) : NotFound();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _authService.GetAllUsersAsync();
            _logger.LogInformation("✓ All users retrieved");
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Failed to get all users");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        try
        {
            var success = await _authService.DeleteUserAsync(userId);
            if (success)
            {
                _logger.LogInformation("✓ User deleted: {UserId}", userId);
                return Ok(new { message = "User deleted" });
            }
            else
            {
                return NotFound(new { error = "User not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Failed to delete user: {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Debug endpoints (no auth)
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "Test endpoint works!" });
    }

    [Authorize]
    [HttpGet("test-protected")]
    public IActionResult TestProtected()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new
        {
            message = "Protected endpoint works!",
            userId,
            email,
            role
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("test-admin")]
    public IActionResult TestAdmin()
    {
        return Ok(new { message = "Admin endpoint works!" });
    }
}
