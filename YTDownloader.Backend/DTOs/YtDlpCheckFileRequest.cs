namespace YTDownloader.Backend.DTOs;

public class YtDlpCheckFileRequest
{
    public required string Url { get; set; }
    public string? QualityLabel { get; set; }
    public string? MediaType { get; set; }
    public string? FileName { get; set; }
}
