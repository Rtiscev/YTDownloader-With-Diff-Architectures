using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using YTDownloader.Backend.DTOs;
using YTDownloader.Backend.Services.Interfaces;
using YTDownloader.Backend.Utils;

namespace YTDownloader.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly IMinioFSService _minioService;
    private readonly ILogger<FileController> _logger;

    public FileController(
        IMinioFSService minioService,
        ILogger<FileController> logger)
    {
        _minioService = minioService;
        _logger = logger;
    }

    [HttpPost("check-exists")]
    public async Task<IActionResult> CheckExists([FromBody] CheckFileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Bucket) || string.IsNullOrWhiteSpace(request.ObjectName))
            return BadRequest(new { success = false, error = "Bucket and ObjectName required" });

        try
        {
            var (exists, size, etag, lastModified) = await _minioService.CheckFileExistsAsync(
                request.Bucket, request.ObjectName);

            if (exists)
            {
                return Ok(new CheckFileResponse
                {
                    Exists = true,
                    Size = size,
                    Etag = etag,
                    LastModified = lastModified
                });
            }

            return Ok(new CheckFileResponse { Exists = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ CheckExists failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("download")]
    public async Task<IActionResult> Download([FromBody] MinioDownloadRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(request.Bucket) || string.IsNullOrWhiteSpace(request.Object))
            return BadRequest(new { error = "Invalid parameters" });

        try
        {
            byte[] fileBytes = await _minioService.DownloadFileAsync(request.Bucket, request.Object);
            _logger.LogInformation("✓ File downloaded by user {UserId}: {Object}", userId, request.Object);
            return File(fileBytes, "application/octet-stream",
                FileHelper.SanitizeFilename(request.Object));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Download failed for user {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("list/{bucketName}")]
    public async Task<IActionResult> List(string bucketName)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            return BadRequest(new { error = "Bucket required" });

        try
        {
            var objects = await _minioService.ListObjectsAsync(bucketName);
            return Ok(objects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ List failed for bucket: {Bucket}", bucketName);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("upload/{bucketName}/{objectName}")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(string bucketName, string objectName, IFormFile file)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        objectName = Uri.UnescapeDataString(objectName);

        if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(objectName) || file?.Length == 0)
            return BadRequest(new { success = false, error = "Invalid parameters" });

        const long maxFileSize = 500 * 1024 * 1024;
        if (file.Length > maxFileSize)
            return BadRequest(new { success = false, error = $"File exceeds {maxFileSize / 1024 / 1024}MB" });

        try
        {
            using var stream = file.OpenReadStream();
            await _minioService.UploadFileAsync(bucketName, objectName, stream, file.Length);
            _logger.LogInformation("✓ File uploaded by user {UserId}: {Object}", userId, objectName);
            return Ok(new { success = true, message = "File uploaded" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Upload failed for user {UserId}", userId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("delete/{bucketName}/{objectName}")]
    public async Task<IActionResult> Delete(string bucketName, string objectName)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        objectName = Uri.UnescapeDataString(objectName);

        if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(objectName))
            return BadRequest(new { success = false, error = "Invalid parameters" });

        try
        {
            await _minioService.DeleteFileAsync(bucketName, objectName);
            _logger.LogInformation("✓ File deleted by admin {UserId}: {Object}", userId, objectName);
            return Ok(new { success = true, message = "File deleted successfully" });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { success = false, error = "File not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Delete failed for admin {UserId}", userId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            const string bucketName = "my-bucket";
            var (totalFiles, totalSize) = await _minioService.GetBucketStatsAsync(bucketName);

            var response = new StatsResponse
            {
                TotalFiles = totalFiles,
                TotalSize = totalSize,
                TotalSizeFormatted = FileHelper.FormatBytes(totalSize),
                BucketName = bucketName,
                Timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Stats failed");
            return StatusCode(500, new
            {
                error = ex.Message,
                totalFiles = 0,
                totalSize = 0,
                totalSizeFormatted = "Error"
            });
        }
    }
}
