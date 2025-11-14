using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net.Http.Headers;
using YtDlpService.Models;
using YtDlpService.DTOs;
using YtDlpService.Services.Interfaces;

namespace YtDlpService.Services.Implementations;

public class DownloadService : IDownloadService
{
    private readonly IYtDlpExecutor _ytDlpService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DownloadService> _logger;

    public DownloadService(
        IYtDlpExecutor ytDlpService,
        IHttpClientFactory httpClientFactory,
        ILogger<DownloadService> logger)
    {
        _ytDlpService = ytDlpService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<CheckFileResponse> CheckFileExistsAsync(CheckFileRequest request)
    {
        try
        {
            string searchFileName = !string.IsNullOrEmpty(request.FileName)
                ? request.FileName
                : await BuildFileNameAsync(request);

            if (string.IsNullOrEmpty(searchFileName))
                return new CheckFileResponse { Exists = false };

            string sanitizedFileName = SanitizeFilename(Path.GetFileNameWithoutExtension(searchFileName));

            using var httpClient = _httpClientFactory.CreateClient();
            var listResponse = await httpClient.GetAsync("http://minioservice:5000/list/my-bucket");

            if (!listResponse.IsSuccessStatusCode)
                return new CheckFileResponse { Exists = false };

            var content = await listResponse.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<List<string>>(content) ?? new();

            var matchingFile = files.FirstOrDefault(f =>
                Path.GetFileNameWithoutExtension(f).Equals(sanitizedFileName, StringComparison.OrdinalIgnoreCase));

            if (matchingFile != null)
            {
                var encodedFileName = Uri.EscapeDataString(matchingFile);
                return new CheckFileResponse
                {
                    Exists = true,
                    FileName = matchingFile,
                    DownloadUrl = $"/api/download-from-minio/my-bucket/{encodedFileName}"
                };
            }

            return new CheckFileResponse { Exists = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CheckFile] Error");
            return new CheckFileResponse { Exists = false };
        }
    }

    public async Task<DownloadAndUploadResponse> DownloadAndUploadAsync(
        DownloadRequest request,
        string bucketName = "my-bucket")
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            string qualityLabel = GetQualityLabel(request);

            var downloadResult = await _ytDlpService.DownloadVideoAsync(request);

            if (!downloadResult.Success || string.IsNullOrEmpty(downloadResult.FilePath))
                throw new InvalidOperationException(downloadResult.ErrorOutput ?? "Download failed");

            var fileName = downloadResult.FileName ?? Path.GetFileName(downloadResult.FilePath);
            string fileNameWithQuality = $"{Path.GetFileNameWithoutExtension(fileName)} [{qualityLabel}]{Path.GetExtension(fileName)}";
            fileNameWithQuality = SanitizeFilename(fileNameWithQuality);

            // Check if exists
            var checkUrl = "http://minioservice:5000/check-exists";
            var checkResponse = await httpClient.PostAsync(checkUrl,
                new StringContent(
                    JsonSerializer.Serialize(new { bucket = bucketName, objectName = fileNameWithQuality }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                )
            );

            if (checkResponse.IsSuccessStatusCode)
            {
                var checkContent = await checkResponse.Content.ReadAsStringAsync();
                var checkResult = JsonSerializer.Deserialize<JsonElement>(checkContent);

                if (checkResult.TryGetProperty("exists", out var existsElement) && existsElement.GetBoolean())
                {
                    try { File.Delete(downloadResult.FilePath); }
                    catch { }

                    var encodedFileName = Uri.EscapeDataString(fileNameWithQuality);
                    return new DownloadAndUploadResponse
                    {
                        Success = true,
                        FileName = fileNameWithQuality,
                        DownloadUrl = $"/api/download-from-minio/{bucketName}/{encodedFileName}",
                        Message = "File already exists"
                    };
                }
            }

            // Upload
            var uploadUrl = $"http://minioservice:5000/upload/{bucketName}/{Uri.EscapeDataString(fileNameWithQuality)}";

            if (!File.Exists(downloadResult.FilePath))
                throw new FileNotFoundException("Downloaded file not found");

            byte[] fileBytes = await File.ReadAllBytesAsync(downloadResult.FilePath);

            using var formData = new MultipartFormDataContent();
            using var byteContent = new ByteArrayContent(fileBytes);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            formData.Add(byteContent, "file", fileNameWithQuality);

            var uploadResponse = await httpClient.PostAsync(uploadUrl, formData);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Upload failed: {errorContent}");
            }

            // Cleanup
            try
            {
                File.Delete(downloadResult.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Download-Upload] Failed to delete local file");
            }

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
            _logger.LogError(ex, "[Download-Upload] Error");
            throw;
        }
    }

    private async Task<string> BuildFileNameAsync(CheckFileRequest request)
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
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"--dump-json --no-warnings \"{url}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            var output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            var json = JsonSerializer.Deserialize<JsonElement>(output);
            return json.TryGetProperty("title", out var titleElement)
                ? titleElement.GetString()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get video title");
            return null;
        }
    }

    private static string SanitizeFilename(string filename) =>
        new(filename.Where(c => c < 128).ToArray());
}