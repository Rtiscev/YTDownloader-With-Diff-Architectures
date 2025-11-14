using Microsoft.AspNetCore.Mvc;
using YtDlpService.Services.Interfaces;
using YtDlpService.Services.Implementations;

namespace YtDlpService.Controllers;

[ApiController]
[Route("system")]
public class SystemController : ControllerBase
{
    private readonly IYtDlpExecutor _ytDlpExecutor;
    private readonly ILogger<SystemController> _logger;

    public SystemController(IYtDlpExecutor ytDlpExecutor, ILogger<SystemController> logger)
    {
        _ytDlpExecutor = ytDlpExecutor;
        _logger = logger;
    }

    [HttpGet("versions")]
    public async Task<IActionResult> GetVersions()
    {
        try
        {
            var ytdlpVersion = await _ytDlpExecutor.GetYtDlpVersionAsync();
            var ffmpegVersion = await _ytDlpExecutor.GetFfmpegVersionAsync();

            return Ok(new
            {
                ytdlp = ytdlpVersion,
                ffmpeg = ffmpegVersion,
                service = "YtDlpService",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Versions] Error");
            return StatusCode(500, new
            {
                error = ex.Message,
                ytdlp = "Error",
                ffmpeg = "Error"
            });
        }
    }
}
