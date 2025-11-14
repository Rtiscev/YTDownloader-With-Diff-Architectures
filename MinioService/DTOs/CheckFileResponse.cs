namespace MinioService.DTOs;

public class CheckFileResponse
{
    public bool Exists { get; set; }
    public long? Size { get; set; }
    public string? Etag { get; set; }
    public DateTime? LastModified { get; set; }
}
