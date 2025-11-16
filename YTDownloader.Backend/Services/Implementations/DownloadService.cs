using Microsoft.Extensions.Logging;
using System.Text.Json;
using YTDownloader.Backend.Models;
using YTDownloader.Backend.DTOs;
using YTDownloader.Backend.Services.Interfaces;
using YTDownloader.Backend.Utils;

namespace YTDownloader.Backend.Services.Implementations;

public class DownloadService : IDownloadService
{
    private readonly IYtDlpExecutor _ytDlpService;
    private readonly IMinioFSService _minioService;
    private readonly ILogger<DownloadService> _logger;

    public DownloadService(
        IYtDlpExecutor ytDlpService,
        IMinioFSService minioService,
        ILogger<DownloadService> logger)
    {
        _ytDlpService = ytDlpService;
        _minioService = minioService;
        _logger = logger;
    }

    public async Task<YtDlpCheckFileResponse> CheckFileExistsAsync(YtDlpCheckFileRequest request)
    {
        try
        {
            string searchFileName = !string.IsNullOrEmpty(request.FileName)
                ? request.FileName
                : await BuildFileNameAsync(request);

            if (string.IsNullOrEmpty(searchFileName))
                return new YtDlpCheckFileResponse { Exists = false };

            string sanitizedFileName = FileHelper.SanitizeFilename(Path.GetFileNameWithoutExtension(searchFileName));

            var files = await _minioService.ListObjectsAsync("my-bucket");

            var matchingFile = files.FirstOrDefault(f =>
                Path.GetFileNameWithoutExtension(f).Equals(sanitizedFileName, StringComparison.OrdinalIgnoreCase));

            if (matchingFile != null)
            {
                var encodedFileName = Uri.EscapeDataString(matchingFile);
                return new YtDlpCheckFileResponse
                {
                    Exists = true,
                    FileName = matchingFile,
                    DownloadUrl = $"/api/download-from-minio/my-bucket/{encodedFileName}"
                };
            }

            return new YtDlpCheckFileResponse { Exists = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ CheckFileExists failed");
            return new YtDlpCheckFileResponse { Exists = false };
        }
    }

    public async Task<DownloadAndUploadResponse> DownloadAndUploadAsync(
        DownloadRequest request,
        string bucketName = "my-bucket")
    {
        string? localFilePath = null;

        try
        {
            string qualityLabel = GetQualityLabel(request);

            // Download video using yt-dlp
            var downloadResult = await _ytDlpService.DownloadVideoAsync(request);

            if (!downloadResult.Success || string.IsNullOrEmpty(downloadResult.FilePath))
            {
                _logger.LogError("✗ yt-dlp download failed: {Error}", downloadResult.ErrorOutput);
                throw new InvalidOperationException(downloadResult.ErrorOutput ?? "Download failed");
            }

            localFilePath = downloadResult.FilePath;
            var fileName = downloadResult.FileName ?? Path.GetFileName(downloadResult.FilePath);
            string fileNameWithQuality = $"{Path.GetFileNameWithoutExtension(fileName)} [{qualityLabel}]{Path.GetExtension(fileName)}";
            fileNameWithQuality = FileHelper.SanitizeFilename(fileNameWithQuality);

            _logger.LogInformation("Downloaded file: {FileName}", fileNameWithQuality);

            // Check if file exists in MinIO
            var (exists, size, etag, lastModified) = await _minioService.CheckFileExistsAsync(bucketName, fileNameWithQuality);

            if (exists)
            {
                _logger.LogInformation("✓ File already exists in MinIO: {FileName}", fileNameWithQuality);

                var encodedFileName = Uri.EscapeDataString(fileNameWithQuality);
                return new DownloadAndUploadResponse
                {
                    Success = true,
                    FileName = fileNameWithQuality,
                    DownloadUrl = $"/api/download-from-minio/{bucketName}/{encodedFileName}",
                    Message = "File already exists"
                };
            }

            // Upload to MinIO
            if (!File.Exists(localFilePath))
                throw new FileNotFoundException("Downloaded file not found", localFilePath);

            var fileInfo = new FileInfo(localFilePath);
            using (var fileStream = File.OpenRead(localFilePath))
            {
                await _minioService.UploadFileAsync(bucketName, fileNameWithQuality, fileStream, fileInfo.Length);
            }

            _logger.LogInformation("✓ File uploaded to MinIO: {FileName}", fileNameWithQuality);

            var encodedFileNameFinal = Uri.EscapeDataString(fileNameWithQuality);
            return new DownloadAndUploadResponse
            {
                Success = true,
                FileName = fileNameWithQuality,
                DownloadUrl = $"/api/download-from-minio/{bucketName}/{encodedFileNameFinal}",
                Message = "File ready"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Download-Upload failed");
            throw;
        }
        finally
        {
            if (!string.IsNullOrEmpty(localFilePath) && File.Exists(localFilePath))
            {
                try
                {
                    // Add small delay to ensure file handles are released
                    await Task.Delay(100);
                    File.Delete(localFilePath);
                    _logger.LogInformation("✓ Local file deleted: {FilePath}", localFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠ Failed to delete local file: {FilePath}", localFilePath);
                }
            }
        }
    }

    private async Task<string> BuildFileNameAsync(YtDlpCheckFileRequest request)
    {
        string videoTitle = await GetVideoTitleAsync(request.Url);
        if (string.IsNullOrEmpty(videoTitle))
            return string.Empty;

        string fileExtension = request.MediaType == "video" ? ".mp4" : ".mp3";
        return !string.IsNullOrEmpty(request.QualityLabel)
            ? $"{videoTitle} [{request.QualityLabel}]{fileExtension}"
            : $"{videoTitle}{fileExtension}";
    }

    private string GetQualityLabel(DownloadRequest request)
    {
        return request.ExtractAudio
            ? request.AudioQuality ?? "192k"
            : !string.IsNullOrEmpty(request.Resolution)
                ? request.Resolution
                : request.Format ?? "unknown";
    }

    private async Task<string?> GetVideoTitleAsync(string url)
    {
        try
        {
            var videoInfo = await _ytDlpService.GetVideoInfoAsync(url);
            return videoInfo.Success ? videoInfo.Title : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠ Failed to get video title for: {Url}", url);
            return null;
        }
    }
}
