using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YtDlpService.Models;
using YtDlpService.Services.Interfaces;
using YtDlpService.Extensions;

namespace YtDlpService.Services.Implementations;

public class YtDlpExecutor : IYtDlpExecutor
{
    private readonly ILogger<IYtDlpExecutor> _logger;
    private readonly string _ytDlpPath;
    private readonly string _downloadBasePath;

    public YtDlpExecutor(ILogger<YtDlpExecutor> logger, IConfiguration configuration)
    {
        _logger = logger;
        _ytDlpPath = configuration["YtDlp:ExecutablePath"] ?? "yt-dlp";

        var configPath = configuration["YtDlp:DownloadPath"];
        _downloadBasePath = string.IsNullOrEmpty(configPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), "downloads")
            : !Path.IsPathRooted(configPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), configPath)
                : configPath;

        try
        {
            if (!Directory.Exists(_downloadBasePath))
                Directory.CreateDirectory(_downloadBasePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create download directory: {Path}", _downloadBasePath);
            throw;
        }
    }

    public async Task<string> GetYtDlpVersionAsync()
    {
        try
        {
            var result = await ExecuteYtDlpAsync("--version");
            return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput)
                ? result.StandardOutput.Trim()
                : "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting yt-dlp version");
            return "Error";
        }
    }

    public async Task<string> GetFfmpegVersionAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return "Unknown";

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                return "Unknown";

            var match = Regex.Match(output, @"ffmpeg version ([\d.]+)");
            return match.Success ? match.Groups[1].Value : output.Split('\n')[0];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ffmpeg version");
            return "Error";
        }
    }

    public async Task<DownloadResponse> DownloadVideoAsync(DownloadRequest request)
    {
        try
        {
            var outputPath = request.OutputPath ?? _downloadBasePath;

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            var outputTemplate = Path.Combine(outputPath, "%(title)s.%(ext)s");
            var arguments = new List<string> { request.Url, "-o", outputTemplate, "--no-playlist" };

            if (request.ExtractAudio)
            {
                if (!string.IsNullOrEmpty(request.Format) && !request.Format.EndsWith("k"))
                {
                    arguments.AddRange(new[] { "-f", request.Format });
                }
                else
                {
                    arguments.AddRange(new[] { "-f", "bestaudio" });
                }

                arguments.AddRange(new[]
                {
                        "-x",
                        "--audio-format",
                        request.AudioFormat ?? "mp3",
                        "--audio-quality",
                        request.AudioQuality ?? "0"
                    });
            }
            else
            {
                string formatToUse = request.Format ?? "bestvideo+bestaudio";

                if (request.MergeAudio && !string.IsNullOrEmpty(request.Format) &&
                    !request.Format.Contains("+") && !request.Format.Contains("bestaudio"))
                {
                    formatToUse = $"{request.Format}+bestaudio";
                }

                arguments.AddRange(new[] { "-f", formatToUse });
            }

            var result = await ExecuteYtDlpAsync(arguments.ToArray());

            if (result.ExitCode != 0)
            {
                _logger.LogError("Download failed: {Error}", result.StandardError);
                return new DownloadResponse
                {
                    Success = false,
                    Message = "Download failed",
                    ErrorOutput = result.StandardError
                };
            }

            var downloadedFile = Directory.GetFiles(outputPath, "*.*")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .FirstOrDefault();

            if (downloadedFile == null)
            {
                _logger.LogError("Download succeeded but file not found");
                return new DownloadResponse
                {
                    Success = false,
                    Message = "Download succeeded but file not found",
                    ErrorOutput = "File not found after download"
                };
            }

            return new DownloadResponse
            {
                Success = true,
                Message = "Download completed successfully",
                OutputFile = outputPath,
                FilePath = downloadedFile,
                FileName = Path.GetFileName(downloadedFile)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during download: {Url}", request.Url);
            return new DownloadResponse
            {
                Success = false,
                Message = "Exception occurred during download",
                ErrorOutput = ex.Message
            };
        }
    }

    public async Task<VideoInfoResponse> GetVideoInfoAsync(string url)
    {
        try
        {
            var result = await ExecuteYtDlpAsync(url, "--dump-json", "--no-playlist");

            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                return new VideoInfoResponse
                {
                    Success = false,
                    ErrorMessage = result.StandardError
                };
            }

            var jsonDoc = JsonDocument.Parse(result.StandardOutput);
            var root = jsonDoc.RootElement;

            var formats = ExtractFormats(root);
            var videoFormats = FilterVideoFormats(formats);

            return new VideoInfoResponse
            {
                Success = true,
                Title = root.GetStringProperty("title"),
                Duration = root.GetInt32Property("duration"),
                Channel = root.GetStringProperty("channel"),
                ChannelFollowerCount = root.GetInt64Property("channel_follower_count"),
                ViewCount = root.GetInt64Property("view_count"),
                LikeCount = root.GetInt64Property("like_count"),
                CommentCount = root.GetInt64Property("comment_count"),
                UploadDate = ParseDate(root, "upload_date"),
                ThumbnailUrl = root.GetStringProperty("thumbnail"),
                AvailableFormats = formats,
                VideoFormats = videoFormats,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video info");
            return new VideoInfoResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    private List<FormatInfo> ExtractFormats(JsonElement root)
    {
        var formats = new List<FormatInfo>();
        if (!root.TryGetProperty("formats", out var formatsArray))
            return formats;

        foreach (var format in formatsArray.EnumerateArray())
        {
            formats.Add(new FormatInfo
            {
                FormatId = format.GetStringProperty("format_id"),
                Format = format.GetStringProperty("format"),
                Extension = format.GetStringProperty("ext"),
                Resolution = format.GetStringProperty("resolution"),
                Filesize = format.GetInt64Property("filesize"),
                Fps = format.GetInt32Property("fps")
            });
        }
        return formats;
    }

    private List<VideoFormat> FilterVideoFormats(List<FormatInfo> formats)
    {
        return formats
            .Where(f => f.Extension == "mp4" && f.Resolution != "audio only" && !string.IsNullOrEmpty(f.Resolution))
            .GroupBy(f => f.Resolution)
            .Select(group => group
                .OrderByDescending(f => f.Filesize ?? 0)
                .ThenByDescending(f => int.TryParse(f.FormatId, out var id) ? id : 0)
                .First()
            )
            .OrderBy(f =>
            {
                var parts = f.Resolution?.Split('x');
                return parts?.Length == 2 && int.TryParse(parts[1], out var height) ? height : 0;
            })
            .Select(f => new VideoFormat
            {
                Id = f.FormatId ?? "",
                Resolution = f.Resolution ?? "",
                Filesize = f.Filesize
            })
            .ToList();
    }

    private DateTime? ParseDate(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var dateElement))
            return null;

        var dateStr = dateElement.GetString();
        return !string.IsNullOrEmpty(dateStr) &&
               DateTime.TryParseExact(dateStr, "yyyyMMdd",
                   System.Globalization.CultureInfo.InvariantCulture,
                   System.Globalization.DateTimeStyles.None, out var parsedDate)
            ? parsedDate
            : null;
    }

    private async Task<ProcessResult> ExecuteYtDlpAsync(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ytDlpPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in arguments)
            startInfo.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = outputBuilder.ToString(),
            StandardError = errorBuilder.ToString()
        };
    }

    private class ProcessResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
    }
}
