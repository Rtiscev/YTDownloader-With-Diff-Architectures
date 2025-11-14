namespace YtDlpService.Models;

public class VideoInfoResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Title { get; set; }
    public int? Duration { get; set; }
    public string? Channel { get; set; }
    public long? ChannelFollowerCount { get; set; }
    public long? ViewCount { get; set; }
    public long? LikeCount { get; set; }
    public long? CommentCount { get; set; }
    public DateTime? UploadDate { get; set; }
    public string? ThumbnailUrl { get; set; }
    public List<FormatInfo> AvailableFormats { get; set; } = new();
    public List<VideoFormat> VideoFormats { get; set; } = new();
}