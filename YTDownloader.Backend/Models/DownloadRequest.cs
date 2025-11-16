namespace YTDownloader.Backend.Models;

public class DownloadRequest
{
    public required string Url { get; set; }
    public string? OutputPath { get; set; }
    public string? Format { get; set; }
    public bool ExtractAudio { get; set; }
    public string? AudioFormat { get; set; }
    public string? AudioQuality { get; set; }
    public string? Resolution { get; set; }
    public bool MergeAudio { get; set; }
}
