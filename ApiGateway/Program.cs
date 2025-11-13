using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load Ocelot configuration
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Register Ocelot services
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// Enable CORS before Ocelot
app.UseCors("AllowAll");

// Health check middleware BEFORE Ocelot
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/health")
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("API Gateway is healthy");
        return;
    }
    await next();
});

// Use Ocelot middleware to handle routing
await app.UseOcelot();
await app.RunAsync();
