using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using YtDlpService.Models;
using YtDlpService.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = null;
});

// Configure form options
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Add Authentication (JWT)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://authservice:5000";
        options.Audience = "ytdlp";
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });

builder.Services.AddAuthorization();

// Add services
builder.Services.AddSingleton<YtDlpExecutor>();
builder.Services.AddHttpClient(Options.DefaultName, client =>
{
    client.Timeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddScoped<DownloadService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Health check
app.MapGet("/", () => Results.Ok(new
{
    service = "YtDlp Microservice",
    version = "1.0",
    status = "running"
}));

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Check file exists
app.MapPost("/check-file", async (CheckFileRequest request, DownloadService downloadService) =>
{
    try
    {
        var result = await downloadService.CheckFileExistsAsync(request);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});

// Download and upload (with auth for premium quality)
app.MapPost("/download-and-upload", async (
    DownloadRequest request,
    DownloadService downloadService,
    HttpContext httpContext,
    IHttpClientFactory httpClientFactory,
    ILogger<Program> logger) =>
{
    try
    {
        bool isHighAudio = request.AudioQuality is "192k" or "320k";
        bool isHighVideo = !string.IsNullOrEmpty(request.Resolution) &&
                          !request.Resolution.Equals("854x480", StringComparison.OrdinalIgnoreCase) &&
                          !request.Resolution.StartsWith("640x") &&
                          !request.Resolution.StartsWith("480x");

        // Verify auth for premium quality
        if (isHighAudio || isHighVideo)
        {
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            var token = authHeader.Replace("Bearer ", "").Trim();

            if (string.IsNullOrEmpty(token))
                return Results.StatusCode(401);

            try
            {
                var httpClient = httpClientFactory.CreateClient();
                var authRequest = new HttpRequestMessage(HttpMethod.Get, "http://authservice:5000/auth/me")
                {
                    Headers = { { "Authorization", $"Bearer {token}" } }
                };

                var authResponse = await httpClient.SendAsync(authRequest);
                if (!authResponse.IsSuccessStatusCode)
                {
                    logger.LogWarning("[Download-Upload] Token validation failed");
                    return Results.StatusCode(401);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Download-Upload] Token validation error");
                return Results.StatusCode(500);
            }
        }

        var result = await downloadService.DownloadAndUploadAsync(request);
        return Results.Ok(new
        {
            success = result.Success,
            fileName = result.FileName,
            downloadUrl = result.DownloadUrl,
            message = result.Message
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[Download-Upload] Error");
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});

// Download
app.MapPost("/download", async (DownloadRequest request, YtDlpExecutor ytDlpService) =>
{
    if (string.IsNullOrWhiteSpace(request.Url))
        return Results.BadRequest(new { error = "URL is required" });

    var result = await ytDlpService.DownloadVideoAsync(request);
    return result.Success
        ? Results.Ok(result)
        : Results.Problem(detail: result.ErrorOutput, statusCode: 500);
});

// Download from MinIO
app.MapGet("/download-from-minio/{bucketName}/{objectName}", async (
    string bucketName,
    string objectName,
    IHttpClientFactory httpClientFactory,
    ILogger<Program> logger) =>
{
    try
    {
        var decodedObjectName = Uri.UnescapeDataString(objectName);

        if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(decodedObjectName))
            return Results.BadRequest(new { error = "Invalid parameters" });

        var httpClient = httpClientFactory.CreateClient();
        var minioUrl = $"http://minioservice:5000/download/{bucketName}/{Uri.EscapeDataString(decodedObjectName)}";

        var response = await httpClient.GetAsync(minioUrl);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("[MinIO Download] MinIO error: {Status}", response.StatusCode);
            return Results.Problem("File not found in MinIO", statusCode: 404);
        }

        var fileBytes = await response.Content.ReadAsByteArrayAsync();
        var sanitizedFileName = SanitizeFilename(decodedObjectName);

        return Results.File(fileBytes, "application/octet-stream", sanitizedFileName);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[MinIO Download] Error");
        return Results.Json(new { error = ex.Message }, statusCode: 500);
    }
});

// Get video info
app.MapGet("/info", async (string url, YtDlpExecutor ytDlpService) =>
{
    if (string.IsNullOrWhiteSpace(url))
        return Results.BadRequest(new { error = "URL parameter is required" });

    var result = await ytDlpService.GetVideoInfoAsync(url);
    return result.Success
        ? Results.Ok(result)
        : Results.Problem(detail: result.ErrorMessage, statusCode: 500);
});

// System versions
app.MapGet("/system/versions", async (YtDlpExecutor executor, ILogger<Program> logger) =>
{
    try
    {
        var ytdlpVersion = await executor.GetYtDlpVersionAsync();
        var ffmpegVersion = await executor.GetFfmpegVersionAsync();

        return Results.Ok(new
        {
            ytdlp = ytdlpVersion,
            ffmpeg = ffmpegVersion,
            service = "YtDlpService",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[Versions] Error");
        return Results.Json(new
        {
            error = ex.Message,
            ytdlp = "Error",
            ffmpeg = "Error"
        }, statusCode: 500);
    }
});

app.Run();

static string SanitizeFilename(string filename) => new(filename.Where(c => c < 128).ToArray());
