using Microsoft.AspNetCore.Mvc;
using MinioService.Services.Implementations;
using MinioService.Services.Interfaces;

namespace MinioService.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IMinioFSService _minioService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IMinioFSService minioService, ILogger<HealthController> logger)
    {
        _minioService = minioService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            bool isHealthy = await _minioService.CheckConnectionAsync();

            if (isHealthy)
                return Ok(new { status = "healthy", message = "MinIO service is running" });
            else
                return StatusCode(503, new { status = "unhealthy", message = "MinIO connection failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Health] Error");
            return StatusCode(503, new { status = "unhealthy", message = ex.Message });
        }
    }
}
