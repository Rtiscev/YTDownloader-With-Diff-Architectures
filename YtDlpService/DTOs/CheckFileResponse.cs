namespace YtDlpService.DTOs;

public class CheckFileResponse
{
    public bool Exists { get; set; }
    public string? FileName { get; set; }
    public string? DownloadUrl { get; set; }
}
