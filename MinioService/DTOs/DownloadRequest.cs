namespace MinioService.DTOs;

public class DownloadRequest
{
    public required string Bucket { get; set; }
    public required string Object { get; set; }
}
