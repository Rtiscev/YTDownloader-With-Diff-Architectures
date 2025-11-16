namespace YTDownloader.Backend.Services.Interfaces;

public interface IMinioFSService
{
    Task<bool> CheckConnectionAsync();
    Task<(bool exists, long? size, string? etag, DateTime? lastModified)> CheckFileExistsAsync(string bucket, string objectName);
    Task<byte[]> DownloadFileAsync(string bucket, string objectName);
    Task<List<string>> ListObjectsAsync(string bucketName);
    Task CreateBucketAsync(string bucketName);
    Task UploadFileAsync(string bucketName, string objectName, Stream stream, long size);
    Task<bool> FileExistsAsync(string bucketName, string objectName);
    Task DeleteFileAsync(string bucketName, string objectName);
    Task<(int totalFiles, long totalSize)> GetBucketStatsAsync(string bucketName);
}
