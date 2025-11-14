namespace MinioService.DTOs;

public class StatsResponse
{
    public int TotalFiles { get; set; }
    public long TotalSize { get; set; }
    public string TotalSizeFormatted { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
