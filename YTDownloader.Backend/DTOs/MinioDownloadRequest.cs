namespace YTDownloader.Backend.DTOs;

public class MinioDownloadRequest
{
    public required string Bucket { get; set; }
    public required string Object { get; set; }
}
