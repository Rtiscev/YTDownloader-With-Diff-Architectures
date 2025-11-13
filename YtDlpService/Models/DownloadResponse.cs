namespace YtDlpService.Models;

public class DownloadRequest
{
    public string Url { get; set; } = string.Empty;
    public string? Format { get; set; }
    public bool ExtractAudio { get; set; }
    public string? AudioFormat { get; set; }
    public string? AudioQuality { get; set; }
    public string? Resolution { get; set; }
    public bool MergeAudio { get; set; } = true; // Default to true
    public string? OutputPath { get; set; }
}


public class DownloadResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? OutputFile { get; set; }
    public string? FilePath { get; set; }  // Add this
    public string? FileName { get; set; }   // Add this
    public string? ErrorOutput { get; set; }
}


public class FormatInfo
{
    public string? FormatId { get; set; }
    public string? Format { get; set; }
    public string? Extension { get; set; }
    public string? Resolution { get; set; }
    public long? Filesize { get; set; }
    public int? Fps { get; set; }
}

public class VideoFormat
{
    public string Id { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public long? Filesize { get; set; }

}
public class VideoInfoResponse
{
    public bool Success { get; set; }
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
    public string? ErrorMessage { get; set; }
}
