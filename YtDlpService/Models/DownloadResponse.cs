namespace YtDlpService.Models;

public class DownloadResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? OutputFile { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public string? ErrorOutput { get; set; }
}