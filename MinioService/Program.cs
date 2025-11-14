using Microsoft.AspNetCore.Http.Features;
using MinioService.Services.Implementations;
using MinioService.Services.Interfaces;


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

// Add controllers
builder.Services.AddControllers();

// Register services
builder.Services.AddSingleton<IMinioFSService, MinioFSService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddHttpClient();

var app = builder.Build();
app.MapControllers();

// Initialize MinIO bucket on startup
using (var scope = app.Services.CreateScope())
{
    var minioService = scope.ServiceProvider.GetRequiredService<IMinioFSService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        const string bucketName = "my-bucket";
        await minioService.CreateBucketAsync(bucketName);
        logger.LogInformation("Bucket '{BucketName}' is ready", bucketName);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error initializing MinIO bucket");
    }
}

app.Run();
