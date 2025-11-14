using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using YtDlpService.DTOs;
using YtDlpService.Models;
using YtDlpService.Services.Interfaces;
using YtDlpService.Services.Implementations;

namespace YtDlpService.Controllers;

[ApiController]
[Route("")]
public class DownloadController : ControllerBase
{
    private readonly IDownloadService _downloadService;
    private readonly IYtDlpExecutor _ytDlpExecutor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DownloadController> _logger;

    public DownloadController(
        IDownloadService downloadService,
        IYtDlpExecutor ytDlpExecutor,
        IHttpClientFactory httpClientFactory,
        ILogger<DownloadController> logger)
    {
        _downloadService = downloadService;
        _ytDlpExecutor = ytDlpExecutor;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("check-file")]
    public async Task<IActionResult> CheckFile([FromBody] CheckFileRequest request)
    {
        try
        {
            var result = await _downloadService.CheckFileExistsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CheckFile] Error");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("download-and-upload")]
    public async Task<IActionResult> DownloadAndUpload([FromBody] DownloadRequest request)
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
                var authHeader = Request.Headers["Authorization"].ToString();
                var token = authHeader.Replace("Bearer ", "").Trim();

                if (string.IsNullOrEmpty(token))
                    return Unauthorized();

                try
                {
                    var httpClient = _httpClientFactory.CreateClient();
                    var authRequest = new HttpRequestMessage(HttpMethod.Get, "http://authservice:5000/auth/me")
                    {
                        Headers = { { "Authorization", $"Bearer {token}" } }
                    };

                    var authResponse = await httpClient.SendAsync(authRequest);
                    if (!authResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("[Download-Upload] Token validation failed");
                        return Unauthorized();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Download-Upload] Token validation error");
                    return StatusCode(500);
                }
            }

            var result = await _downloadService.DownloadAndUploadAsync(request);
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
            _logger.LogError(ex, "[Download-Upload] Error");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("download")]
    public async Task<IActionResult> Download([FromBody] DownloadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(new { error = "URL is required" });

        var result = await _ytDlpExecutor.DownloadVideoAsync(request);
        return result.Success
            ? Ok(result)
            : Problem(detail: result.ErrorOutput, statusCode: 500);
    }

    [HttpGet("download-from-minio/{bucketName}/{objectName}")]
    public async Task<IActionResult> DownloadFromMinio(string bucketName, string objectName)
    {
        try
        {
            var decodedObjectName = Uri.UnescapeDataString(objectName);

            if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(decodedObjectName))
                return BadRequest(new { error = "Invalid parameters" });

            var httpClient = _httpClientFactory.CreateClient();
            var minioUrl = $"http://minioservice:5000/download/{bucketName}/{Uri.EscapeDataString(decodedObjectName)}";

            var response = await httpClient.GetAsync(minioUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[MinIO Download] MinIO error: {Status}", response.StatusCode);
                return Problem("File not found in MinIO", statusCode: 404);
            }

            var fileBytes = await response.Content.ReadAsByteArrayAsync();
            var sanitizedFileName = SanitizeFilename(decodedObjectName);

            return File(fileBytes, "application/octet-stream", sanitizedFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MinIO Download] Error");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private static string SanitizeFilename(string filename) =>
        new(filename.Where(c => c < 128).ToArray());
}
