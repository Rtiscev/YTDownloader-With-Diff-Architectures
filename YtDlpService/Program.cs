using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using YtDlpService.Services.Interfaces;
using YtDlpService.Services.Implementations;

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

// Register services with interfaces
builder.Services.AddScoped<IYtDlpExecutor, YtDlpExecutor>();
builder.Services.AddScoped<IDownloadService, DownloadService>();
builder.Services.AddHttpClient();

var app = builder.Build();
app.MapControllers();

app.Run();
