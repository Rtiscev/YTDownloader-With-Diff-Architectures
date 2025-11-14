namespace YtDlpService.DTOs;

public class CheckFileRequest
{
    public required string Url { get; set; }
    public string? QualityLabel { get; set; }
    public string? MediaType { get; set; }
    public string? FileName { get; set; }
}
