namespace YTDownloader.Backend.DTOs;

public class YtDlpCheckFileResponse
{
    public bool Exists { get; set; }
    public string? FileName { get; set; }
    public string? DownloadUrl { get; set; }
}
