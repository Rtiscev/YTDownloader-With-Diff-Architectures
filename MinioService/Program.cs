using Minio;
using Minio.DataModel.Args;
using Microsoft.AspNetCore.Http.Features;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = null;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

var configuration = builder.Configuration;
var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

string minioUser = configuration["MINIO_ROOT_USER"] ?? "minioadmin200";
string minioPassword = configuration["MINIO_ROOT_PASSWORD"] ?? "minioadmin";
string minioEndpoint = configuration["MINIO_ENDPOINT"] ?? "minio:9000";
string authServiceUrl = configuration["AUTH_SERVICE_URL"] ?? "http://apigateway:5000/api/auth/me";

var minio = new MinioClient()
    .WithEndpoint(minioEndpoint)
    .WithCredentials(minioUser, minioPassword)
    .WithSSL(false)
    .Build();

async Task<(bool valid, string? error, bool isAdmin)> VerifyAuthAsync(HttpRequest request, ILogger log)
{
    var authHeader = request.Headers["Authorization"].ToString();

    if (string.IsNullOrWhiteSpace(authHeader))
        return (false, "Unauthorized", false);

    if (!authHeader.StartsWith("Bearer "))
        return (false, "Invalid authorization format", false);

    var token = authHeader["Bearer ".Length..];
    if (string.IsNullOrWhiteSpace(token))
        return (false, "No token provided", false);

    try
    {
        var httpClient = new HttpClient();
        var authRequest = new HttpRequestMessage(HttpMethod.Get, authServiceUrl)
        {
            Headers = { { "Authorization", authHeader } }
        };

        var authResponse = await httpClient.SendAsync(authRequest);

        if (!authResponse.IsSuccessStatusCode)
        {
            return authResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? (false, "Unauthorized - Invalid token", false)
                : (false, "Auth service error", false);
        }

        var json = await authResponse.Content.ReadAsStringAsync();
        var userData = JsonSerializer.Deserialize<JsonElement>(json);

        if (userData.ValueKind == JsonValueKind.Object)
        {
            if (userData.TryGetProperty("role", out var roleProp))
                return (true, null, roleProp.GetString() == "Admin");

            if (userData.TryGetProperty("roles", out var rolesProp) && rolesProp.ValueKind == JsonValueKind.Array)
            {
                var isAdmin = rolesProp.EnumerateArray().Any(r => r.GetString() == "Admin");
                return (true, null, isAdmin);
            }
        }

        return (true, null, false);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "[Auth] Error verifying token");
        return (false, "Auth verification failed", false);
    }
}

// Health check
app.MapGet("/health", async (ILogger<Program> log) =>
{
    try
    {
        await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket("test"));
        return Results.Ok(new { status = "healthy", message = "MinIO service is running" });
    }
    catch (Exception ex)
    {
        log.LogError(ex, "[Health] Error");
        return Results.Json(new { status = "unhealthy", message = ex.Message }, statusCode: 503);
    }
});

// Check if file exists
app.MapPost("/check-exists", async (CheckFileRequest req, ILogger<Program> log) =>
{
    if (string.IsNullOrWhiteSpace(req.Bucket) || string.IsNullOrWhiteSpace(req.ObjectName))
        return Results.BadRequest(new { success = false, error = "Bucket and ObjectName required" });

    try
    {
        var stat = await minio.StatObjectAsync(new StatObjectArgs()
            .WithBucket(req.Bucket)
            .WithObject(req.ObjectName));
        return Results.Ok(new { exists = true, size = stat.Size, etag = stat.ETag, lastModified = stat.LastModified });
    }
    catch (Minio.Exceptions.ObjectNotFoundException)
    {
        return Results.Ok(new { exists = false });
    }
    catch (Exception ex)
    {
        log.LogError(ex, "[CheckExists] Error");
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});

// Download
app.MapPost("/download", async (DownloadRequest req, ILogger<Program> log) =>
{
    if (string.IsNullOrWhiteSpace(req.Bucket) || string.IsNullOrWhiteSpace(req.Object))
        return Results.BadRequest(new { error = "Invalid parameters" });

    try
    {
        string tempDir = "/tmp/downloads";
        Directory.CreateDirectory(tempDir);
        string tempFile = Path.Combine(tempDir, Guid.NewGuid().ToString());

        await minio.GetObjectAsync(new GetObjectArgs()
            .WithBucket(req.Bucket)
            .WithObject(req.Object)
            .WithFile(tempFile));

        byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(tempFile);

        try { System.IO.File.Delete(tempFile); }
        catch { }

        if (fileBytes.Length == 0)
            return Results.Json(new { error = "Downloaded file is empty" }, statusCode: 500);

        return Results.File(fileBytes, "application/octet-stream", SanitizeFilename(req.Object));
    }
    catch (Exception ex)
    {
        log.LogError(ex, "[Download] Error");
        return Results.Json(new { error = ex.Message }, statusCode: 500);
    }
});

// List
app.MapGet("/list/{bucketName}", async (string bucketName, ILogger<Program> log) =>
{
    if (string.IsNullOrWhiteSpace(bucketName))
        return Results.BadRequest(new { error = "Bucket required" });

    try
    {
        var objects = new List<string>();
        await foreach (var obj in minio.ListObjectsEnumAsync(new ListObjectsArgs().WithBucket(bucketName)))
            objects.Add(obj.Key);

        return Results.Ok(objects);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "[List] Error");
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});

// Create Bucket (ADMIN ONLY)
app.MapPost("/create-bucket/{bucketName}", async (HttpRequest request, string bucketName, ILogger<Program> log) =>
{
    var (valid, error, isAdmin) = await VerifyAuthAsync(request, log);
    if (!valid) return Results.Json(new { error }, statusCode: 401);
    if (!isAdmin) return Results.Json(new { error = "Forbidden: Admin role required" }, statusCode: 403);

    if (string.IsNullOrWhiteSpace(bucketName) || bucketName.Length < 3 || bucketName.Length > 63)
        return Results.BadRequest(new { success = false, error = "Invalid bucket name" });

    try
    {
        await minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
        return Results.Ok(new { success = true, message = $"Bucket '{bucketName}' created" });
    }
    catch (Exception ex)
    {
        log.LogError(ex, "[CreateBucket] Error");
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});

// Upload
app.MapPost("/upload/{bucketName}/{objectName}", async (string bucketName, string objectName, IFormFile file, ILogger<Program> log) =>
{
    objectName = Uri.UnescapeDataString(objectName);

    if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(objectName) || file?.Length == 0)
        return Results.BadRequest(new { success = false, error = "Invalid parameters" });

    const long maxFileSize = 500 * 1024 * 1024;
    if (file.Length > maxFileSize)
        return Results.BadRequest(new { success = false, error = $"File exceeds {maxFileSize / 1024 / 1024}MB" });

    try
    {
        using var stream = file.OpenReadStream();
        await minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(file.Length));

        return Results.Ok(new { success = true, message = "File uploaded" });
    }
    catch (Exception ex)
    {
        log.LogError(ex, "[Upload] Error");
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
}).DisableAntiforgery();

// Delete (ADMIN ONLY)
app.MapDelete("/delete/{bucketName}/{objectName}", async (HttpRequest request, string bucketName, string objectName, ILogger<Program> log) =>
{
    var (valid, error, isAdmin) = await VerifyAuthAsync(request, log);
    if (!valid) return Results.Json(new { error }, statusCode: 401);
    if (!isAdmin) return Results.Json(new { error = "Forbidden: Admin role required" }, statusCode: 403);

    objectName = Uri.UnescapeDataString(objectName);

    if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(objectName))
        return Results.BadRequest(new { success = false, error = "Invalid parameters" });

    try
    {
        try
        {
            await minio.StatObjectAsync(new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName));
        }
        catch
        {
            return Results.NotFound(new { success = false, error = "File not found" });
        }

        await minio.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName));

        return Results.Ok(new { success = true, message = "File deleted successfully", verified = true });
    }
    catch (Exception ex)
    {
        log.LogError(ex, "[Delete] Error");
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});

// Stats (ADMIN ONLY)
app.MapGet("/stats", async (HttpRequest request, ILogger<Program> log) =>
{
    var (valid, error, isAdmin) = await VerifyAuthAsync(request, log);
    if (!valid) return Results.Json(new { error }, statusCode: 401);
    if (!isAdmin) return Results.Json(new { error = "Forbidden: Admin role required" }, statusCode: 403);

    try
    {
        const string bucketName = "my-bucket";
        long totalSize = 0;
        int totalFiles = 0;

        await foreach (var obj in minio.ListObjectsEnumAsync(new ListObjectsArgs()
            .WithBucket(bucketName)
            .WithRecursive(true)))
        {
            if (obj != null)
            {
                totalSize += (long)obj.Size;
                totalFiles++;
            }
        }

        return Results.Ok(new
        {
            totalFiles,
            totalSize,
            totalSizeFormatted = FormatBytes(totalSize),
            bucketName,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        log.LogError(ex, "[Stats] Error");
        return Results.Json(new { error = ex.Message, totalFiles = 0, totalSize = 0, totalSizeFormatted = "Error" }, statusCode: 500);
    }
});

app.Run();

static string SanitizeFilename(string filename) => new(filename.Where(c => c < 128).ToArray());

static string FormatBytes(long bytes)
{
    string[] sizes = { "B", "KB", "MB", "GB", "TB" };
    double len = bytes;
    int order = 0;
    while (len >= 1024 && order < sizes.Length - 1)
    {
        order++;
        len /= 1024;
    }
    return $"{len:F2} {sizes[order]}";
}

public class CheckFileRequest
{
    public required string Bucket { get; set; }
    public required string ObjectName { get; set; }
}

public class DownloadRequest
{
    public required string Bucket { get; set; }
    public required string Object { get; set; }
}
