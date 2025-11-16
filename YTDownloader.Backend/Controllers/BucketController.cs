using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YTDownloader.Backend.Services.Implementations;
using YTDownloader.Backend.Services.Interfaces;

namespace YTDownloader.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BucketController : ControllerBase
{
    private readonly IMinioFSService _minioService;
    private readonly ILogger<BucketController> _logger;

    public BucketController(
        IMinioFSService minioService,
        ILogger<BucketController> logger)
    {
        _minioService = minioService;
        _logger = logger;
    }

    [HttpPost("create/{bucketName}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateBucket(string bucketName)
    {
        if (string.IsNullOrWhiteSpace(bucketName) || bucketName.Length < 3 || bucketName.Length > 63)
            return BadRequest(new { success = false, error = "Invalid bucket name" });

        try
        {
            await _minioService.CreateBucketAsync(bucketName);
            _logger.LogInformation("[CreateBucket] Bucket '{BucketName}' created by user {UserId}", bucketName, User.FindFirst("sub")?.Value);
            return Ok(new { success = true, message = $"Bucket '{bucketName}' created" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateBucket] Error creating bucket '{BucketName}'", bucketName);
            return StatusCode(500, new { success = false, error = "Failed to create bucket" });
        }
    }
}
