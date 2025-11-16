using DotNetEnv;
using DotNetEnv.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using YTDownloader.Backend.Data;
using YTDownloader.Backend.Models;
using YTDownloader.Backend.Services.Implementations;
using YTDownloader.Backend.Services.Interfaces;

// Load .env file FIRST (before anything else)
DotNetEnv.Env.Load();
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDotNetEnv(".env", DotNetEnv.LoadOptions.TraversePath());

// ===== GET ALL VALUES FROM .env FILE =====
var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Startup");

var connectionString = DotNetEnv.Env.GetString("POSTGRES_CONNECTION_STRING", "");
var jwtIssuer = DotNetEnv.Env.GetString("Jwt_Issuer", "");
var jwtAudience = DotNetEnv.Env.GetString("Jwt_Audience", "");
var jwtSecret = DotNetEnv.Env.GetString("Jwt_Secret", "");
var adminEmail = DotNetEnv.Env.GetString("Admin_Email", "");
var adminPassword = DotNetEnv.Env.GetString("Admin_Password", "");

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


//builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IDownloadService, DownloadService>();
builder.Services.AddScoped<IMinioFSService, MinioFSService>();
builder.Services.AddScoped<IYtDlpExecutor, YtDlpExecutor>();


// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!))
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

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

var app = builder.Build();

// Use CORS
app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.EnsureCreated();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create roles
        foreach (var role in new[] { "Admin", "User", "Guest" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create default admin user
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Admin",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Admin user created: {Email}", adminEmail);
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during database seeding");
    }
}

// Initialize MinIO bucket on startup
using (var scope = app.Services.CreateScope())
{
    var minioService = scope.ServiceProvider.GetRequiredService<IMinioFSService>();

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
