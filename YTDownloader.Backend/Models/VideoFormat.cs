namespace YTDownloader.Backend.Models;

public class VideoFormat
{
    public string Id { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public long? Filesize { get; set; }
}
