using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using YTDownloader.Backend.DTOs;
using YTDownloader.Backend.Models;
using YTDownloader.Backend.Services.Interfaces;
using YTDownloader.Backend.Utils;

namespace YTDownloader.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    private readonly IDownloadService _downloadService;
    private readonly IYtDlpExecutor _ytDlpExecutor;
    private readonly IMinioFSService _minioService;
    private readonly ILogger<DownloadController> _logger;

    public DownloadController(
        IDownloadService downloadService,
        IYtDlpExecutor ytDlpExecutor,
        IMinioFSService minioService,
        ILogger<DownloadController> logger)
    {
        _downloadService = downloadService;
        _ytDlpExecutor = ytDlpExecutor;
        _minioService = minioService;
        _logger = logger;
    }

    [HttpPost("check-file")]
    public async Task<IActionResult> CheckFile([FromBody] YtDlpCheckFileRequest request)
    {
        try
        {
            var result = await _downloadService.CheckFileExistsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ CheckFile failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("download-and-upload")]
    public async Task<IActionResult> DownloadAndUpload([FromBody] Models.DownloadRequest request)
    {
        try
        {
            bool isHighAudio = request.AudioQuality is "192k" or "320k";
            bool isHighVideo = !string.IsNullOrEmpty(request.Resolution) &&
                              !request.Resolution.Equals("854x480", StringComparison.OrdinalIgnoreCase) &&
                              !request.Resolution.StartsWith("640x") &&
                              !request.Resolution.StartsWith("480x");

            // Verify auth for premium quality
            if (isHighAudio || isHighVideo)
            {
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    _logger.LogWarning("⚠ Unauthorized access attempt for premium quality");
                    return Unauthorized(new { error = "Authentication required for premium quality" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("✓ Premium download by user {UserId}", userId);
            }

            var result = await _downloadService.DownloadAndUploadAsync(request);

            if (result.Success)
                _logger.LogInformation("✓ Download completed: {FileName}", result.FileName);
            else
                _logger.LogWarning("✗ Download failed: {Message}", result.Message);

            return Ok(new
            {
                success = result.Success,
                fileName = result.FileName,
                downloadUrl = result.DownloadUrl,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Download-Upload failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("download")]
    public async Task<IActionResult> Download([FromBody] DownloadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(new { error = "URL is required" });

        var result = await _ytDlpExecutor.DownloadVideoAsync(request);

        if (result.Success)
            _logger.LogInformation("✓ Video downloaded: {Url}", request.Url);
        else
            _logger.LogError("✗ Download failed: {Url}", request.Url);

        return result.Success
            ? Ok(result)
            : Problem(detail: result.ErrorOutput, statusCode: 500);
    }

    [HttpGet("info")]
    public async Task<IActionResult> GetVideoInfo([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { error = "URL parameter is required" });

        var result = await _ytDlpExecutor.GetVideoInfoAsync(url);
        return result.Success
            ? Ok(result)
            : Problem(detail: result.ErrorMessage, statusCode: 500);
    }

    [HttpGet("download-from-minio/{bucketName}/{objectName}")]
    public async Task<IActionResult> DownloadFromMinio(string bucketName, string objectName)
    {
        try
        {
            var decodedObjectName = Uri.UnescapeDataString(objectName);

            if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(decodedObjectName))
                return BadRequest(new { error = "Invalid parameters" });

            _logger.LogInformation("Downloading from MinIO: {Bucket}/{Object}", bucketName, decodedObjectName);

            // ✅ Use MinIO service directly instead of HTTP call
            var fileBytes = await _minioService.DownloadFileAsync(bucketName, decodedObjectName);

            var sanitizedFileName = FileHelper.SanitizeFilename(decodedObjectName);

            _logger.LogInformation("✓ File downloaded from MinIO: {FileName}", sanitizedFileName);

            return File(fileBytes, "application/octet-stream", sanitizedFileName);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning("⚠ File not found in MinIO: {Bucket}/{Object}", bucketName, objectName);
            return NotFound(new { error = "File not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ MinIO download failed: {Bucket}/{Object}", bucketName, objectName);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
