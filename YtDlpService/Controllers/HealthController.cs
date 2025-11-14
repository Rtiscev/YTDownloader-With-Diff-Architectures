using Microsoft.AspNetCore.Mvc;

namespace YtDlpService.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetRoot()
    {
        return Ok(new
        {
            service = "YtDlp Microservice",
            version = "1.0",
            status = "running"
        });
    }

    [HttpGet("api/health")]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
