namespace YTDownloader.Backend.Models;

public class FormatInfo
{
    public string? FormatId { get; set; }
    public string? Format { get; set; }
    public string? Extension { get; set; }
    public string? Resolution { get; set; }
    public long? Filesize { get; set; }
    public int? Fps { get; set; }
}
