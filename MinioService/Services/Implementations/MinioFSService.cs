using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using MinioService.Services.Interfaces;

namespace MinioService.Services.Implementations;

public class MinioFSService : IMinioFSService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioFSService> _logger;

    public MinioFSService(IConfiguration configuration, ILogger<MinioFSService> logger)
    {
        _logger = logger;

        string minioUser = configuration["MINIO_ROOT_USER"] ?? "minioadmin200";
        string minioPassword = configuration["MINIO_ROOT_PASSWORD"] ?? "minioadmin";
        string minioEndpoint = configuration["MINIO_ENDPOINT"] ?? "minio:9000";

        _minioClient = new MinioClient()
            .WithEndpoint(minioEndpoint)
            .WithCredentials(minioUser, minioPassword)
            .WithSSL(false)
            .Build();
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket("test"));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MinIO] Connection check failed");
            return false;
        }
    }

    public async Task<(bool exists, long? size, string? etag, DateTime? lastModified)> CheckFileExistsAsync(
        string bucket, string objectName)
    {
        try
        {
            var stat = await _minioClient.StatObjectAsync(new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName));
            return (true, stat.Size, stat.ETag, stat.LastModified);
        }
        catch (ObjectNotFoundException)
        {
            return (false, null, null, null);
        }
    }

    public async Task<byte[]> DownloadFileAsync(string bucket, string objectName)
    {
        string tempDir = "/tmp/downloads";
        Directory.CreateDirectory(tempDir);
        string tempFile = Path.Combine(tempDir, Guid.NewGuid().ToString());

        try
        {
            await _minioClient.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithFile(tempFile));

            byte[] fileBytes = await File.ReadAllBytesAsync(tempFile);

            if (fileBytes.Length == 0)
                throw new InvalidOperationException("Downloaded file is empty");

            return fileBytes;
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    public async Task<List<string>> ListObjectsAsync(string bucketName)
    {
        var objects = new List<string>();
        await foreach (var obj in _minioClient.ListObjectsEnumAsync(
            new ListObjectsArgs().WithBucket(bucketName)))
        {
            objects.Add(obj.Key);
        }
        return objects;
    }

    public async Task CreateBucketAsync(string bucketName)
    {
        try
        {
            // Check if bucket already exists
            bool bucketExists = await _minioClient.BucketExistsAsync(
                new Minio.DataModel.Args.BucketExistsArgs().WithBucket(bucketName));

            if (!bucketExists)
            {
                await _minioClient.MakeBucketAsync(
                    new Minio.DataModel.Args.MakeBucketArgs().WithBucket(bucketName));
                _logger.LogInformation("Bucket '{BucketName}' created successfully", bucketName);
            }
            else
            {
                _logger.LogInformation("Bucket '{BucketName}' already exists", bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bucket '{BucketName}'", bucketName);
            throw;
        }
    }


    public async Task UploadFileAsync(string bucketName, string objectName, Stream stream, long size)
    {
        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(size));
    }

    public async Task<bool> FileExistsAsync(string bucketName, string objectName)
    {
        try
        {
            await _minioClient.StatObjectAsync(new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName));
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
    }

    public async Task DeleteFileAsync(string bucketName, string objectName)
    {
        // Check if file exists first
        var exists = await FileExistsAsync(bucketName, objectName);
        if (!exists)
            throw new FileNotFoundException("File not found");

        await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName));
    }

    public async Task<(int totalFiles, long totalSize)> GetBucketStatsAsync(string bucketName)
    {
        long totalSize = 0;
        int totalFiles = 0;

        await foreach (var obj in _minioClient.ListObjectsEnumAsync(new ListObjectsArgs()
            .WithBucket(bucketName)
            .WithRecursive(true)))
        {
            if (obj != null)
            {
                totalSize += (long)obj.Size;
                totalFiles++;
            }
        }

        return (totalFiles, totalSize);
    }
}
