using Microsoft.AspNetCore.Mvc;
using YTDownloader.Backend.Services.Interfaces;
using YTDownloader.Backend.Services.Implementations;

namespace YTDownloader.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
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
                service = "YTDownloader.Backend",
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
