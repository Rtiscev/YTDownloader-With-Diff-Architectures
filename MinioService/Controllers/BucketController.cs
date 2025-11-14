using Microsoft.AspNetCore.Mvc;
using MinioService.Services.Implementations;
using MinioService.Services.Interfaces;

namespace MinioService.Controllers;

[ApiController]
[Route("[controller]")]
public class BucketController : ControllerBase
{
    private readonly IMinioFSService _minioService;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<BucketController> _logger;

    public BucketController(
        IMinioFSService minioService,
        IAuthenticationService authService,
        ILogger<BucketController> logger)
    {
        _minioService = minioService;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("create/{bucketName}")]
    public async Task<IActionResult> CreateBucket(string bucketName)
    {
        var (valid, error, isAdmin) = await _authService.VerifyAuthAsync(Request);
        if (!valid) return Unauthorized(new { error });
        if (!isAdmin) return StatusCode(403, new { error = "Forbidden: Admin role required" });

        if (string.IsNullOrWhiteSpace(bucketName) || bucketName.Length < 3 || bucketName.Length > 63)
            return BadRequest(new { success = false, error = "Invalid bucket name" });

        try
        {
            await _minioService.CreateBucketAsync(bucketName);
            return Ok(new { success = true, message = $"Bucket '{bucketName}' created" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateBucket] Error");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}
