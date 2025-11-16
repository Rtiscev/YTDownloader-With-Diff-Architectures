namespace YTDownloader.Backend.DTOs;

public class CheckFileRequest
{
    public required string Bucket { get; set; }
    public required string ObjectName { get; set; }
}