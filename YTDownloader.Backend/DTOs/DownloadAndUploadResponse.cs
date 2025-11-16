namespace YTDownloader.Backend.DTOs;

public class DownloadAndUploadResponse
{
    public bool Success { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
