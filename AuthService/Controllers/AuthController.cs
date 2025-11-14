using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AuthService.DTOs.Auth;
using AuthService.DTOs.Responses;
using AuthService.Services.Interfaces;

namespace AuthService.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _authService.RegisterAsync(request);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _authService.LoginAsync(request);
        return response.Success ? Ok(response) : Unauthorized(response);
    }

    // [HttpPost("refresh")]
    // public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    // {
    //     var response = await _authService.RefreshTokenAsync(request);
    //     return response.Success ? Ok(response) : Unauthorized(response);
    // }

    // [Authorize]
    // [HttpPost("logout")]
    // public async Task<IActionResult> Logout()
    // {
    //     var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //     if (userId == null)
    //         return Unauthorized();

    //     var success = await _authService.LogoutAsync(userId);
    //     return success ? Ok(new { message = "Logged out successfully" }) : BadRequest();
    // }

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

    // [Authorize(Roles = "Admin")]
    // [HttpGet("users/{userId}")]
    // public async Task<IActionResult> GetUser(string userId)
    // {
    //     var user = await _authService.GetUserAsync(userId);
    //     return user != null ? Ok(user) : NotFound();
    // }

    [Authorize(Roles = "Admin")]
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }



    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "Test endpoint works!" });
    }

    [HttpGet("test-auth")]
    public IActionResult TestAuth()
    {
        var authHeader = Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(authHeader))
            return BadRequest(new { message = "No authorization header" });

        return Ok(new
        {
            message = "Auth header received",
            header = authHeader.Substring(0, 20) + "..."
        });
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
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(new
        {
            message = "Admin endpoint works!",
            userId
        });
    }
}
